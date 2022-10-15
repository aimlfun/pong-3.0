using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong;

/// <summary>
/// Which side the bat is positioned.
/// </summary>
enum Side { Left, Right };

/// <summary>
/// Represents a bat.
/// 
/// c_batDistanceFromEdgeX
///     X
/// +---+
/// 
///     ||  -+
///     ||   | c_halfTheBatLength
///     ||   |
///     ##  -+  Y
///     ||
///     ||
///     ||
/// 
/// </summary>
internal class Bat
{
    /// <summary>
    /// How far the bat is from the edge.
    /// </summary>
    internal const int c_batDistanceFromEdgeX = 30;

    /// <summary>
    /// Length of bat is double this. We draw up this much, plus down this much.
    /// </summary>
    internal const int c_halfTheBatLength = 16;

    /// <summary>
    /// How much zone the bat cannot reach (ball placed here wins game).
    /// </summary>
    internal const int c_deadZone = 8;

    /// <summary>
    /// Tracks whether this is a left or right bat.
    /// </summary>
    private readonly Side sideBatAppearsOn;

    /// <summary>
    /// Y position of the bat center.
    /// </summary>
    internal int Y;

    /// <summary>
    /// X position of the bat.
    /// </summary>
    internal int X;

    /// <summary>
    /// Height of the court.
    /// </summary>
    private readonly int HeightPX;

    /// <summary>
    /// Pen for drawing the bat (solid thick line).
    /// </summary>
    private readonly Pen penBat = new(Color.FromArgb(240, 255, 255, 255), 6);

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="side"></param>
    /// <param name="height"></param>
    internal Bat(Side side, int width, int height)
    {
        sideBatAppearsOn = side;
        HeightPX = height;

        Y = height / 2;

        // depending on side, we position to the left or right
        X = (side == Side.Left) ?
                    (c_batDistanceFromEdgeX + (int)penBat.Width / 2) :
                    (width - c_batDistanceFromEdgeX - (int)penBat.Width / 2);
    }

    /// <summary>
    /// Moves the "bat" smoothly upto a fixed amount
    /// </summary>
    /// <param name="yTarget"></param>
    internal void Move(int yTarget)
    {
        // clamp so it moves a maximum of +/- 3 pixels in any go.
        // also clamp so it cannot empty the dead zone.
        Y = yTarget.Clamp(Y - 3, Y + 3).Clamp(c_deadZone + c_halfTheBatLength, HeightPX - (c_deadZone + c_halfTheBatLength));
    }

    /// <summary>
    /// Detects where on the bat it hit.
    /// </summary>
    /// <param name="ballY"></param>
    /// <returns></returns>
    internal int BallHitBat(int ballY)
    {
        float dist = (ballY - Y);

        // did not hit the bat
        if (Math.Abs(dist) > c_halfTheBatLength) return int.MaxValue;

        // bat is split into 8 zones
        dist /= (c_halfTheBatLength / 4f);

        return (int)Math.Round(dist);
    }

    /// <summary>
    /// Draws the bat.
    /// </summary>
    /// <param name="graphics"></param>
    internal void Draw(Graphics graphics)
    {
        graphics.DrawLine(penBat, new Point(X, Y - c_halfTheBatLength), new Point(X, Y + c_halfTheBatLength));
    }
}