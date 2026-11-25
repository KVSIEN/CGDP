using System.Collections.Generic;
using UnityEngine;

namespace CGD.Algorithms
{
    // Turtle graphics: reads a string of commands (an LSystem's output) as moves of a pen, and
    // returns the lines it draws.
    //
    // How it works: a "turtle" has a position and a heading. It walks the string symbol by symbol:
    //     F or G   step forward, drawing a line;
    //     f        step forward without drawing;
    //     +   -    turn left / right by the turn angle;
    //     [   ]    remember where you are and which way you face / go back to the last remembered
    //              spot: the start and end of a branch;
    //     anything else is ignored (an L-system's X, for example, only steers the growth).
    //
    // Why it's useful: it turns an L-system's text into geometry: lines to place vine, root or
    // branch meshes along, decals to stamp, or points to spawn leaves at. The lines are 2D, on a
    // plane of your choosing (a wall's face, the floor).
    public static class Turtle2D
    {
        // The lines drawn, from the origin, starting `startHeading` degrees anticlockwise from +x
        // (90 = straight up).
        public static List<(Vector2 from, Vector2 to)> Draw(string commands, float stepLength, float turnDegrees, float startHeading = 90f)
        {
            var lines = new List<(Vector2, Vector2)>();
            var saved = new Stack<(Vector2 position, float heading)>();
            Vector2 position = Vector2.zero;
            float heading = startHeading;

            foreach (char command in commands)
            {
                switch (command)
                {
                    case 'F':
                    case 'G':
                        Vector2 end = position + Direction(heading) * stepLength;
                        lines.Add((position, end));
                        position = end;
                        break;
                    case 'f':
                        position += Direction(heading) * stepLength;
                        break;
                    case '+': heading += turnDegrees; break;
                    case '-': heading -= turnDegrees; break;
                    case '[': saved.Push((position, heading)); break;
                    case ']':
                        if (saved.Count > 0) (position, heading) = saved.Pop();
                        break;
                }
            }
            return lines;
        }

        private static Vector2 Direction(float degrees) =>
            new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
    }
}
