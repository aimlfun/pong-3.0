using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;

namespace Pong;

public partial class FormPong : Form
{
    /// <summary>
    /// true - it will draw lines showing the "dead" zone and the "goal" line.
    /// </summary>
    private const bool c_drawDebugLines = false;

    /// <summary>
    /// Training generation.
    /// </summary>
    private int epoch = 0;

    /// <summary>
    /// Left "bat".
    /// </summary>
    private readonly Bat batOnTheLeftControlledByAI;

    /// <summary>
    /// Right "bat".
    /// </summary>
    private readonly Bat batOnTheRightControlledByHumanOrTrainer;

    /// <summary>
    /// Implements a 2 player scoreboard.
    /// </summary>
    private readonly ScoreBoard scoreBoard = new();

    /// <summary>
    /// Pen for drawing the net (dashed line).
    /// </summary>
    private static readonly Pen penNet = new(Color.FromArgb(100, 255, 255, 255), 1);

    /// <summary>
    /// Pen for drawing the cursor, which is represented as a red square.
    /// </summary>
    private static readonly Pen penCursor = new(Color.FromArgb(100, 255, 0, 0));

    /// <summary>
    /// The target position for the right hand bat.
    /// </summary>
    private int yTarget;

    /// <summary>
    /// A "ball" for this game.
    /// </summary>
    private Ball ball;

    /// <summary>
    /// Where the cursor is (mouse move) or where the auto mode positions it.
    /// </summary>
    private Point cursorPosition = new();

    /// <summary>
    /// The current training item (assigned when the ball reaches rhs).
    /// </summary>
    private TrainingDataItem? currentTrainingDataItem = null;

    /// <summary>
    /// The neural network controlling the bat.
    /// </summary>
    private readonly NeuralNetwork neuralNetworkControllingLeftBat;

    /// <summary>
    /// In auto-mode, it's playing against itself. The bat on the right is a "ball-tracker" not AI.
    /// </summary>
    private bool inAutoMode = true;

    /// <summary>
    /// In quiet mode it doesn't paint the screen. This enables it train a lot quicker
    /// </summary>
    private bool inQuietMode = false;

    /// <summary>
    /// Used to ensure in quiet mode it occasionally yields to Windows so it is not unresponsive.
    /// </summary>
    private int hiddenRefreshCount = 0;

    /// <summary>
    /// Perpendicular hits are a bit of a waste of time, so we mostly let it choose to hit with the outer edges.
    /// </summary>
    int offsetChosenByTrainerToStopPerpendicularHits = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
#pragma warning disable CS8618 // SPURIOUS WARNING. NewBall() populates it.
    public FormPong()
#pragma warning restore CS8618 // SPURIOUS WARNING. NewBall() populates it.
    {
        InitializeComponent();

        batOnTheLeftControlledByAI = new(Side.Left, pictureBoxDisplay.Width, pictureBoxDisplay.Height);
        batOnTheRightControlledByHumanOrTrainer = new(Side.Right, pictureBoxDisplay.Width, pictureBoxDisplay.Height);
        yTarget = batOnTheRightControlledByHumanOrTrainer.Y;

        NewBall();

        penNet.DashPattern = new[] { 5f, 5f };

        neuralNetworkControllingLeftBat = new(new int[] { 4, 4, 4, 4, 4, 1 });
        
        TrainingDataItem.Load();

        for (int i = 0; i < 1000; i++) Train();

        timer1.Enabled = true;
    }

    /// <summary>
    /// Ball reached bat on left-hand-side. Did it hit the bat? 
    /// </summary>
    private void Ball_BallReachedBatLeft()
    {
        // if "yposition" == -1, then we haven't stored the position, and need to do so then save the training data.
        if (currentTrainingDataItem is not null && currentTrainingDataItem.YPositionOfTheBallWhenItArrivesOnLeftBatLine == -1)
        {
            currentTrainingDataItem.YPositionOfTheBallWhenItArrivesOnLeftBatLine = ball.Y;

            // don't save ones that end up in the dead-zone. No point in learning this accurately.
            if (ball.Y > Bat.c_deadZone + Bat.c_halfTheBatLength && ball.Y < pictureBoxDisplay.Height - Bat.c_deadZone - Bat.c_halfTheBatLength)
            {
                TrainingDataItem.AddToTrainingData(currentTrainingDataItem);
            }
        }

        int whereBallHitBat = batOnTheLeftControlledByAI.BallHitBat((int)Math.Round(ball.Y));

        // maxValue = missed or  ball is behind bat
        if (whereBallHitBat == int.MaxValue || ball.X < Bat.c_batDistanceFromEdgeX - 3)
        {
            if (ball.X > 4) return; // we're keep moving the ball off behind bat

            // ball missed the bat and has gone off behind the bat a little.

            // left player missed, so right player gets a point.
            scoreBoard.RightPlayerScored();

            // introduce a slight pause, and train the AI based on what it has.
            for (int i = 0; i < 100; i++) Train();

            // lob a new ball

            NewBall();
            return;
        }

        // ball hit the bat, ensure ball starts to right of left bat
        ball.X = Bat.c_batDistanceFromEdgeX + 4;

        // inverts the direction and makes it go off in a hit centric direction
        ball.BounceBallOffBat(whereBallHitBat);

        SetOffsetOfTrainer();
    }

    /// <summary>
    /// Ball reached bat on right-hand-side. Did it hit the bat? 
    /// </summary>
    private void Ball_BallReachedBatRight()
    {
        // determine where the ball hit the bat, MaxValue means "missed" bat
        int whereBallHitBat = batOnTheRightControlledByHumanOrTrainer.BallHitBat((int)Math.Round(ball.Y));

        // missed or is to right of the bat.
        if (whereBallHitBat == int.MaxValue || ball.X > pictureBoxDisplay.Width - Bat.c_batDistanceFromEdgeX + 3)
        {
            if (ball.X < Width - 4) return;

            scoreBoard.LeftPlayerScored();

            for (int i = 0; i < 100; i++) Train();

            NewBall();
            return;
        }

        ball.X = pictureBoxDisplay.Width - (Bat.c_batDistanceFromEdgeX + 7);

        ball.BounceBallOffBat(whereBallHitBat);

        // track item
        currentTrainingDataItem = new TrainingDataItem
        {
            YPositionOfTheBallWhenItHitsBat = ball.Y, // this impacts where it hit with respect to bat, and therefore return angle
            YPositionOfOppositionsBat = batOnTheRightControlledByHumanOrTrainer.Y,
            SpeedOfTheBallWhenItHitsBatX = ball.dx,
            SpeedOfTheBallWhenItHitsBatY = ball.dy,

            // the one below is stored when it reaches the "left" bat / goal line.
            YPositionOfTheBallWhenItArrivesOnLeftBatLine = -1
        };
    }

    /// <summary>
    /// Creates a new ball.
    /// </summary>
    private void NewBall()
    {
        ++epoch;

        ball = new Ball(pictureBoxDisplay.Width, pictureBoxDisplay.Height, BallDirection.LeftToRight);

        // events for when the ball reaches either bat
        ball.BallReachedBatLeft += Ball_BallReachedBatLeft;
        ball.BallReachedBatRight += Ball_BallReachedBatRight;

        SetOffsetOfTrainer(); // picks a random offset so bat doesn't return perpendicular balls

        Text = $"Pong 3.0 - Epoch {epoch}";
    }

    /// <summary>
    /// For each returned or new ball, the trainer picks an offset.
    /// </summary>
    private void SetOffsetOfTrainer()
    {
        int delta = RandomNumberGenerator.GetInt32(-Bat.c_halfTheBatLength, Bat.c_halfTheBatLength);
        
        offsetChosenByTrainerToStopPerpendicularHits = delta;
    }

    /// <summary>
    /// Frame by frame animation, using a timer.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer1_Tick(object sender, EventArgs e)
    {
        if (inQuietMode) timer1.Enabled = false;

        do
        {
            MoveBallAndBothBats();

            if (!inQuietMode) UpdateVideoDisplay();

            // train every frame!
            if (TrainingDataItem.Count < 1000) Train(); // as the training points increase, performance will suffer

            if (inQuietMode && (++hiddenRefreshCount % 10) == 0) Application.DoEvents(); // quiet mode will make form unresponsive without this.
        }
        while (inQuietMode);

        timer1.Enabled = true;
    }

    /// <summary>
    /// Makes an empty display, draws the net, 2xbats + ball + score
    /// </summary>
    private void UpdateVideoDisplay()
    {
        Bitmap b = new(pictureBoxDisplay.Width, pictureBoxDisplay.Height);

        using Graphics g = Graphics.FromImage(b);
        g.Clear(Color.Black);

        // draw net (goes vertical)
        g.DrawLine(penNet, pictureBoxDisplay.Width / 2, 0, pictureBoxDisplay.Width / 2, pictureBoxDisplay.Height);

        // draw a square where the cursor is horizontally
        if (!inAutoMode) g.DrawRectangle(penCursor, batOnTheRightControlledByHumanOrTrainer.X - 3, cursorPosition.Y - 3, 6, 6);

        // draw both bats + ball
        batOnTheLeftControlledByAI.Draw(g);
        batOnTheRightControlledByHumanOrTrainer.Draw(g);

        // ball gets drawn, then moved. This leads to a more Pong like visual plus a faster ball.
        ball.Draw(g);
        ball.Move();
        ball.Draw(g);

        scoreBoard.Draw(g, pictureBoxDisplay.Width);

        if (c_drawDebugLines)
        {
#pragma warning disable CS0162 // Unreachable code detected. The code is reached if you turn on debug.
            DrawDebugLines(g);
#pragma warning restore CS0162 // Unreachable code detected. The code is reached if you turn on debug.
        }

        pictureBoxDisplay.Image?.Dispose();
        pictureBoxDisplay.Image = b;
    }

    /// <summary>
    /// Draw debug lines.
    /// </summary>
    /// <param name="g"></param>
    private void DrawDebugLines(Graphics g)
    {
        using Pen penDebugLine = new(Color.FromArgb(70, 255, 0, 0));
        penDebugLine.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

        // lines where bats are
        g.DrawLine(penDebugLine, Bat.c_batDistanceFromEdgeX + 7, 0, Bat.c_batDistanceFromEdgeX + 7, pictureBoxDisplay.Height);
        g.DrawLine(penDebugLine, pictureBoxDisplay.Width - Bat.c_batDistanceFromEdgeX - 7, 0, pictureBoxDisplay.Width - Bat.c_batDistanceFromEdgeX - 7, pictureBoxDisplay.Height);

        // lines showing "dead" zone that bat cannot enter
        g.DrawLine(penDebugLine, 0, Bat.c_deadZone, pictureBoxDisplay.Width, Bat.c_deadZone);
        g.DrawLine(penDebugLine, 0, pictureBoxDisplay.Height - Bat.c_deadZone, pictureBoxDisplay.Width, pictureBoxDisplay.Height - Bat.c_deadZone);
    }

    /// <summary>
    /// Moves the ball and bats.
    /// </summary>
    private void MoveBallAndBothBats()
    {
        // now move the ball.
        ball.Move();

        if (inAutoMode)
        {
            cursorPosition.Y = (int)ball.Y + offsetChosenByTrainerToStopPerpendicularHits;
            yTarget = cursorPosition.Y;
        }

        // either human controlled, or auto
        batOnTheRightControlledByHumanOrTrainer.Move(yTarget);
        batOnTheRightControlledByHumanOrTrainer.Move(yTarget);

        // neural network controls left bat.
        if (currentTrainingDataItem is not null)
        {
            batOnTheLeftControlledByAI.Move((int)Math.Round(neuralNetworkControllingLeftBat.FeedForward(currentTrainingDataItem.ToArray(pictureBoxDisplay.Height))[0] * Height));
            batOnTheLeftControlledByAI.Move((int)Math.Round(neuralNetworkControllingLeftBat.FeedForward(currentTrainingDataItem.ToArray(pictureBoxDisplay.Height))[0] * Height));
        }
    }

    /// <summary>
    /// Trains the "bat".
    /// </summary>
    private void Train()
    {
        foreach (TrainingDataItem tdi in TrainingDataItem.TrainingData)
        {
            neuralNetworkControllingLeftBat.BackPropagate(tdi.ToArray(pictureBoxDisplay.Height), new double[] { tdi.YPositionOfTheBallWhenItArrivesOnLeftBatLine / Height });
        }
    }

    /// <summary>
    /// If automode, it tracks the ball automatically. 
    /// If !automode, user is expected to use the mouse to control the bat.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PictureBoxDisplay_MouseMove(object sender, MouseEventArgs e)
    {
        if (!inAutoMode)
        {
            Control? c = FindControlAtCursor(this);

            if (c == null || c != pictureBoxDisplay) return;

            if (this.Focused) yTarget = e.Y;
            cursorPosition = e.Location;
        }
        else
        {
            cursorPosition.Y = (int)(ball.Y + RandomNumberGenerator.GetInt32(-10, 10));
            yTarget = cursorPosition.Y;
        }
    }

    /// <summary>
    /// Handle special keys.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FormPong_KeyDown(object sender, KeyEventArgs e)
    {
        // "P" pauses game
        if (e.KeyCode == Keys.P) timer1.Enabled = !timer1.Enabled;

        // "S" cycles speed.
        if (e.KeyCode == Keys.S) StepThroughSpeeds();

        // "A" turns on auto-return mode.
        if (e.KeyCode == Keys.A)
        {
            inAutoMode = !inAutoMode;
            if (inAutoMode) Cursor.Hide(); else Cursor.Show();
        }

        // "Q" toggles quiet mode (fast learn)
        if (e.KeyCode == Keys.Q) inQuietMode = !inQuietMode;
    }

    /// <summary>
    /// Steps thru the speeds by changing the timer interval.
    /// </summary>
    internal void StepThroughSpeeds()
    {
        var newInterval = timer1.Interval switch
        {
            5 => 20,
            20 => 100,
            100 => 1000,
            _ => 5,
        };

        timer1.Interval = newInterval;
    }

    /// <summary>
    /// Works out from the control hierarchy where the point is situated.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Control? FindControlAtPoint(Control container, Point pos)
    {
        Control? child;

        foreach (Control c in container.Controls)
        {
            if (c.Visible && c.Bounds.Contains(pos))
            {
                child = FindControlAtPoint(c, new Point(pos.X - c.Left, pos.Y - c.Top));

                if (child == null)
                    return c;
                else
                    return child;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns a control at the current cursor position of "null" if not within form.
    /// </summary>
    /// <param name="form"></param>
    /// <returns></returns>
    public static Control? FindControlAtCursor(Form form)
    {
        Point pos = Cursor.Position;

        if (form.Bounds.Contains(pos)) return FindControlAtPoint(form, form.PointToClient(pos));

        return null;
    }
}