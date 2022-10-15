using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pong;

/// <summary>
/// 
/// </summary>
internal enum BallDirection { LeftToRight, RightToLeft };


/// <summary>
/// 
/// </summary>
internal class Ball
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="zone"></param>
    internal delegate void Notify();  // delegate

    /// <summary>
    /// 
    /// </summary>
    internal event Notify? BallReachedBatLeft;

    /// <summary>
    /// 
    /// </summary>
    internal event Notify? BallReachedBatRight;

    /// <summary>
    /// 
    /// </summary>
    internal float X;

    /// <summary>
    /// 
    /// </summary>
    internal float Y;

    /// <summary>
    /// 
    /// </summary>
    internal float dx;

    /// <summary>
    /// 
    /// </summary>
    internal float dy;

    /// <summary>
    /// 
    /// </summary>
    internal float accel = 1.05f;//5% faster

    /// <summary>
    /// 
    /// </summary>
    private readonly int Height;

    /// <summary>
    /// 
    /// </summary>
    private readonly int Width;

    /// <summary>
    /// Constructor. For a new ball.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="direction"></param>
    internal Ball(int width, int height, BallDirection direction)
    {
        Height = height;
        Width = width;

        X = width / 2;
        Y = height / 2;

        dy = ((float)(RandomNumberGenerator.GetInt32(300, 2500) / 100f) * (RandomNumberGenerator.GetInt32(1, 2) > 1 ? -1 : 1)) / 10f;
        dx = ((float)(RandomNumberGenerator.GetInt32(900, 2500) / 100f) * (direction == BallDirection.LeftToRight ? 1 : -1)) / 10f;
        //dx = (direction == BallDirection.RightToLeft ? 1 : -1;
    }

    /// <summary>
    /// Moves the ball.
    /// </summary>
    internal void Move()
    {
        // move the ball
        X += dx;
        Y += dy;

        // bounce of top
        if (Y < 0)
        {
            dy = -dy;
            Y = 0 + dy;
        }
        else
        // bounce of bottom
        if (Y > Height)
        {
            dy = -dy;
            Y = Height + dy;
        }

        // if the ball has reached the left goal-line, we fire the event
        if (X < Bat.c_batDistanceFromEdgeX + 7)
        {
            BallReachedBatLeft?.Invoke();
        }
        // if the ball has reached the right goal-line, we fire the event
        else if (X > Width - Bat.c_batDistanceFromEdgeX - 7)
        {
            BallReachedBatRight?.Invoke();
        }
    }

    /// <summary>
    /// Draw the ball. Sure we could draw a round one, but that would not be "authentic".
    /// </summary>
    /// <param name="graphics"></param>
    internal void Draw(Graphics graphics)
    {
        using SolidBrush brush = new(Color.FromArgb(150, 255, 255, 255));
        graphics.FillRectangle(brush, X - 2, Y - 2, 4, 4);
    }

    /// <summary>
    /// Ball bounces off bat.
    /// </summary>
    /// <param name="hit"></param>
    internal void BounceBallOffBat(int hit)
    {
        X -= dx; // move away from being inside the bat

        dx = -dx;  // switch direction (right->left, or left->right)
        dy = hit / 2f;

        // speed it up in x & y direction
        dx *= accel;
        dy *= accel;

        // without this, it could cause it to exponentially speed up.
        dx = dx.Clamp(-10, 10);
        dy = dy.Clamp(-10, 10);
    }
}