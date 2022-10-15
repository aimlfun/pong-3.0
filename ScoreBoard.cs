using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong
{
    /// <summary>
    /// Implements a score-board with large-digits.
    /// Pong is either: winning score to either be 11 points or 15 points.
    /// </summary>
    internal class ScoreBoard
    {
        /// <summary>
        /// Score for the left hand player
        /// </summary>
        int leftPlayerScore = 0;

        Bitmap[]? leftScoreCachedImage = null;

        /// <summary>
        /// Score for the right hand player
        /// </summary>
        int rightPlayerScore = 0;

        Bitmap[]? rightScoreCachedImage = null;

        /// <summary>
        /// Increment the left hand player's score.
        /// </summary>
        internal void LeftPlayerScored()
        {
            leftPlayerScore++;

            if (leftPlayerScore > 99) leftPlayerScore = 0;

            leftScoreCachedImage = null;
        }

        /// <summary>
        /// Increment the right hand player's score.
        /// </summary>
        internal void RightPlayerScored()
        {
            rightPlayerScore++;

            if (rightPlayerScore > 99) rightPlayerScore = 0;
            
            rightScoreCachedImage = null;
        }

        /// <summary>
        /// Resets the score.
        /// </summary>
        internal void Reset()
        {
            leftPlayerScore = 0;
            rightPlayerScore = 0;

            leftScoreCachedImage = null;
            rightScoreCachedImage = null;
        }

        /// <summary>
        /// Draws 2 digit score for each player.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="Width"></param>
        internal void Draw(Graphics graphics, int Width)
        {
            if (leftScoreCachedImage is null)
            {
                leftScoreCachedImage = new[] {
                    Digit.GetImage((int)(leftPlayerScore / 10)),
                    Digit.GetImage(leftPlayerScore % 10)
                };
            }

            if (rightScoreCachedImage is null)
            {
                rightScoreCachedImage = new[] {
                    Digit.GetImage((int)(rightPlayerScore / 10)),
                    Digit.GetImage(rightPlayerScore % 10)
                };
            }

            // Atari clone positions were like this.
            //              width
            //     |-40      0      |30
            //         |-25      |10
            //     15 5 15 10 10 15 5 15 
            //     ### ###   |   ### ### 
            //     # # # #   |   # # # #          
            //     # # # #   |   # # # #          
            //     # # # #   |   # # # #          
            //     ### ###   |   ### ###    
            //         -20
            //     [-]
            //     15

            // But original Pong has digits aligning to the bat's
            // which tbh look a bit rubbish as left when less than 10, looks misaligned. Either side of center looks better

            if (leftPlayerScore > 9) graphics.DrawImageUnscaled(leftScoreCachedImage[0], Bat.c_batDistanceFromEdgeX + 10, 10);
            graphics.DrawImageUnscaled(leftScoreCachedImage[1], Bat.c_batDistanceFromEdgeX + 30, 10);

            if (rightPlayerScore > 9) graphics.DrawImageUnscaled(rightScoreCachedImage[0], Width - Bat.c_batDistanceFromEdgeX + 5 - 15 - 20 - 15, 10);
            graphics.DrawImageUnscaled(rightScoreCachedImage[1], Width - Bat.c_batDistanceFromEdgeX + 5 - 15 - 15, 10);
        }
    }
}