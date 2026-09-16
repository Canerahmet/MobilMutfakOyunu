using System;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Save;
using Lokanta.Core.Sim;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>A summary of one save slot; the slot screen shows this.</summary>
    public struct SlotInfo
    {
        public bool Exists;
        public string Cuisine;
        public int Day;
        public long Cash;
        public int ReputationCenti;
        public DateTime Saved;
        /// <summary>The file is there but cannot be read. Showing it silently as empty is wrong.</summary>
        public bool Broken;
    }

    /// <summary>
    /// Four save slots, on disk. The four-slot design of docs/21.
    ///
    /// Two files are written: the state and the SUMMARY. The summary is
    /// separate because the slot screen must not have to load all four
    /// saves - a full save is ~100 KB and reading four of them at once
    /// would slow the menu down.
    ///
    /// The write is ATOMIC: a .tmp first, then a replace. An application
    /// that dies mid-save must not leave half a file behind; half a save
    /// means the player's whole campaign.
    /// </summary>
    public static class SaveStore
    {
        public const int SlotCount = 4;

        // THE NAMES ON DISK ARE STILL TURKISH. They are not source a
        // reader has to understand; they are a contract with the
        // player's device - renaming the folder, or the state and
        // summary file names, loses every save already written there.
        private static string Dir
        {
            get { return Path.Combine(Application.persistentDataPath, "kayit"); }
        }

        private static string StatePath(int slot)
        {
            return Path.Combine(Dir, "yuva" + slot + ".json");
        }

        private static string InfoPath(int slot)
        {
            return Path.Combine(Dir, "yuva" + slot + ".ozet.json");
        }

        // ---------------------------------------------------------------------
        public static SlotInfo Read(int slot)
        {
            SlotInfo info = new SlotInfo();
            string path = InfoPath(slot);
            if (!File.Exists(path)) return info;

            try
            {
                string[] parts = File.ReadAllText(path).Split('');
                info.Exists = true;
                info.Cuisine = parts[0];
                // The INVARIANT culture: a save file has to be portable between
                // machines and must not become unreadable when the device's
                // language changes.
                System.Globalization.CultureInfo inv =
                    System.Globalization.CultureInfo.InvariantCulture;

                info.Day = int.Parse(parts[1], inv);
                info.Cash = long.Parse(parts[2], inv);
                info.ReputationCenti = int.Parse(parts[3], inv);
                info.Saved = new DateTime(long.Parse(parts[4], inv), DateTimeKind.Utc);

                // THE VERSION IS IN THE SUMMARY TOO.
                //
                // The state file and the summary are written SEPARATELY and the
                // summary did not carry the version. When the game is updated and
                // SaveVersion goes up, the slot card shows a healthy campaign -
                // the day, the till, the reputation - the player presses
                // "Continue" and Restore throws. Next time it is opened the slot
                // looks healthy again.
                //
                // The field is not there in OLD saves; if it is missing no version
                // check is made and the behaviour stays as it was. If it is there
                // and does not match, the slot HONESTLY shows as broken.
                if (parts.Length > 5)
                {
                    int v;
                    if (int.TryParse(parts[5], System.Globalization.NumberStyles.Integer,
                                     inv, out v)
                        && v != Simulation.SaveVersion)
                    {
                        info.Broken = true;
                    }
                }
            }
            catch (Exception e)
            {
                // A broken summary DOES NOT mean a broken save - but because we
                // cannot tell the two apart we show the slot as "broken".
                // Showing it silently as "empty" would have let the player write
                // over it and really lose it.
                Debug.LogWarning("could not read the save summary (slot " + slot + "): " + e.Message);
                info.Exists = true;
                info.Broken = true;
            }
            return info;
        }

        public static bool Save(int slot, Simulation sim, string cuisine)
        {
            try
            {
                Directory.CreateDirectory(Dir);

                JsonStateWriter w = new JsonStateWriter();
                sim.Write(w);
                WriteAtomic(StatePath(slot), w.ToJson());

                System.Globalization.CultureInfo inv =
                    System.Globalization.CultureInfo.InvariantCulture;

                string info = string.Join("", new[]
                {
                    cuisine,
                    sim.Day.ToString(inv),
                    sim.Cash.ToString(inv),
                    sim.ReputationCenti.ToString(inv),
                    DateTime.UtcNow.Ticks.ToString(inv),
                    Simulation.SaveVersion.ToString(inv),
                });
                WriteAtomic(InfoPath(slot), info);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("could not write the save (slot " + slot + "): " + e);
                return false;
            }
        }

        /// <summary>
        /// Loads the slot. The simulation must ALREADY have been built with
        /// the right cuisine; Restore throws if the cuisine does not match.
        /// </summary>
        public static bool Load(int slot, Simulation sim)
        {
            try
            {
                string path = StatePath(slot);
                if (!File.Exists(path)) return false;
                sim.Restore(new JsonStateReader(File.ReadAllText(path)));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("could not load the save (slot " + slot + "): " + e);
                return false;
            }
        }

        public static void Delete(int slot)
        {
            try
            {
                if (File.Exists(StatePath(slot))) File.Delete(StatePath(slot));
                if (File.Exists(InfoPath(slot))) File.Delete(InfoPath(slot));
            }
            catch (Exception e)
            {
                Debug.LogError("could not delete the save (slot " + slot + "): " + e);
            }
        }

        /// <summary>
        /// A REALLY atomic write.
        ///
        /// The previous version was NOT atomic, and its name said it was:
        ///
        ///     File.WriteAllText(tmp, text);              // not forced to disk
        ///     if (File.Exists(path)) File.Delete(path);  // THE OLD SAVE IS GONE
        ///     File.Move(tmp, path);
        ///
        /// There were two separate ways to lose the lot. The first: if the
        /// application dies between the delete and the move, there is neither
        /// a new save nor an old one on disk - the campaign has gone. The
        /// second: WriteAllText returns as soon as the data has landed in the
        /// page cache, so if the battery dies at that moment the .tmp is left
        /// half written and next time the game is opened that half file
        /// stands in for the real save - and the good copy was deleted a step
        /// earlier.
        ///
        /// As it stands now: FORCE the write to disk first (Flush(true) goes
        /// all the way down to the operating system), then swap it in with a
        /// SINGLE rename. A rename is indivisible in the file system;
        /// whichever moment we die at, either the old save or the new save is
        /// on the disk, and there is no state in between.
        /// </summary>
        private static void WriteAtomic(string path, string text)
        {
            string tmp = path + ".tmp";

            using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs, Utf8))
            {
                sw.Write(text);
                sw.Flush();
                fs.Flush(true);
            }

            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        /// <summary>
        /// UTF-8 without a BOM. StreamWriter's default writes a BOM and those
        /// three bytes break JObject.Parse.
        /// </summary>
        private static readonly System.Text.UTF8Encoding Utf8 =
            new System.Text.UTF8Encoding(false);
    }
}
