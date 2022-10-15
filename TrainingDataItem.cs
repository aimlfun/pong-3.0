using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pong;

/// <summary>
/// Represents a Training Data Item
/// </summary>
internal class TrainingDataItem
{
    /// <summary>
    /// Where we write / expect the training data.
    /// </summary>
    private const string c_trainingDataFilePath = @"c:\temp\pong.txt";

    /// <summary>
    /// List of training items recorded (or loaded and added to).
    /// </summary>
    private static readonly List<TrainingDataItem> s_trainingData = new();

    /// <summary>
    /// Used to prevent it adding the same item more than once.
    /// </summary>
    private static readonly HashSet<string> s_uniqueTrainingData = new();

    /// <summary>
    /// The vertical position of the opposition's bat.
    /// </summary>
    internal double YPositionOfOppositionsBat;

    /// <summary>
    /// The vertical position of the ball when it hits the bat
    /// </summary>
    internal double YPositionOfTheBallWhenItHitsBat;

    /// <summary>
    /// The speed of the ball in the horizontal direction.
    /// </summary>
    internal double SpeedOfTheBallWhenItHitsBatX;

    /// <summary>
    /// The speed of the ball in the vertical direction.
    /// </summary>
    internal double SpeedOfTheBallWhenItHitsBatY;

    /// <summary>
    /// The "Y" position of the ball when it arrives on the left.
    /// </summary>
    internal double YPositionOfTheBallWhenItArrivesOnLeftBatLine;

    /// <summary>
    /// Returns the training data item as a string.
    /// </summary>
    /// <returns></returns>
    public override string? ToString()
    {
        return $"{YPositionOfOppositionsBat},{YPositionOfTheBallWhenItHitsBat},{SpeedOfTheBallWhenItHitsBatX},{SpeedOfTheBallWhenItHitsBatY},{YPositionOfTheBallWhenItArrivesOnLeftBatLine}";
        //               0                           1                                       2                               3                                    4                
    }

    /// <summary>
    /// Creates an array of the training data, used for training.
    /// </summary>
    /// <returns></returns>
    internal double[] ToArray(float height)
    {
        return new double[] { YPositionOfOppositionsBat / height, YPositionOfTheBallWhenItHitsBat / height, SpeedOfTheBallWhenItHitsBatX / 10, SpeedOfTheBallWhenItHitsBatY / 10 };
    }

    /// <summary>
    /// Returns the number of items.
    /// </summary>
    internal static int Count
    {
        get { return s_trainingData.Count; }
    }

    /// <summary>
    /// Returns the training data.
    /// </summary>
    internal static List<TrainingDataItem> TrainingData
    {
        get
        {
            return s_trainingData;
        }
    }

    /// <summary>
    /// Convert serialised training data back into an object.
    /// </summary>
    /// <returns></returns>
    internal static TrainingDataItem Deserialise(string line)
    {
        string[] tokens = line.Split(",");

        TrainingDataItem item = new()
        {
            YPositionOfOppositionsBat = float.Parse(tokens[0]),
            YPositionOfTheBallWhenItHitsBat = float.Parse(tokens[1]),
            SpeedOfTheBallWhenItHitsBatX = float.Parse(tokens[2]),
            SpeedOfTheBallWhenItHitsBatY = float.Parse(tokens[3]),
            YPositionOfTheBallWhenItArrivesOnLeftBatLine = float.Parse(tokens[4])
        };

        return item;
    }

    /// <summary>
    /// Stores the training data into a list, and then saves to file.
    /// </summary>
    internal static void AddToTrainingData(TrainingDataItem? item, bool save = true)
    {
        if (item is null) return;

        string? itemText = item.ToString();
        if (string.IsNullOrEmpty(itemText)) return;

        // have we already got this exact data?
        if (s_uniqueTrainingData.Contains(itemText)) return; // no need to store

        s_uniqueTrainingData.Add(itemText);

        // expand training data 
        s_trainingData.Add(item);

        // save every 100
        if (save && s_uniqueTrainingData.Count % 100 == 0) File.WriteAllText(c_trainingDataFilePath, string.Join("\n", s_trainingData));
    }

    /// <summary>
    /// Loads the training file.
    /// </summary>
    internal static void Load()
    {
        // the training data is optional. If not present there is nothing to load.
        if (!File.Exists(c_trainingDataFilePath)) return;

        string[] lines = File.ReadAllLines(c_trainingDataFilePath);

        foreach (string line in lines)
        {
            TrainingDataItem.AddToTrainingData(TrainingDataItem.Deserialise(line), false);
        }
    }
}