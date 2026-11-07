using System;
using System.IO;
using UnityEngine;

namespace CGD.Saving
{
    // One save on disk, as JSON. Writes go to a temporary file that then replaces the save, so
    // a crash or full disk mid-write leaves the previous save intact. Never throws on I/O:
    // the game keeps playing, and the caller decides what a failed read or write means.
    public class SaveFile
    {
        private const string TempSuffix      = ".tmp";
        private const string UnreadableSuffix = ".unreadable";

        public string Path { get; }

        public bool Exists => File.Exists(Path);

        public SaveFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("A save needs a file path.", nameof(path));
            Path = path;
        }

        // False when there is no save, or it can't be read or parsed.
        public bool TryRead<T>(out T data) where T : class
        {
            data = null;
            if (!Exists) return false;

            try
            {
                string json = File.ReadAllText(Path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                return false;
            }
        }

        public bool Write<T>(T data) where T : class
        {
            string temp = Path + TempSuffix;
            try
            {
                string directory = System.IO.Path.GetDirectoryName(Path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(temp, JsonUtility.ToJson(data, prettyPrint: true));
                if (Exists) File.Replace(temp, Path, null);
                else        File.Move(temp, Path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PlatformNotSupportedException)
            {
                return false;
            }
        }

        // Moves a save that can't be used out of the way (next to it, as *.unreadable) instead of
        // overwriting it, so it can still be looked at or recovered by hand.
        public void SetAsideUnreadable()
        {
            if (!Exists) return;
            try
            {
                string aside = Path + UnreadableSuffix;
                if (File.Exists(aside)) File.Delete(aside);
                File.Move(Path, aside);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
        }

        public void Delete()
        {
            try
            {
                if (Exists) File.Delete(Path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { }
        }
    }
}
