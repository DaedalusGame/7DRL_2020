using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading;

namespace RoguelikeEngine
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if !DEBUG
            try
            {
#endif
                using (var game = new Game())
                    game.Run();
#if !DEBUG
            }
            catch (Exception e)
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                StreamWriter write = File.CreateText("crashdump_" + DateTime.Now.ToString("ddMMyyyyhhmmss") + ".txt");

                write.WriteLine("I am so sorry ;(");
                write.WriteLine("---------------");
                write.WriteLine(e.Source);
                write.WriteLine("---------------");
                write.WriteLine(e.GetType().ToString());
                write.WriteLine("at " + e.TargetSite);
                write.WriteLine(e.StackTrace + "\n");
                write.WriteLine("" + e.Message + "\n");
                write.WriteLine("Inner: " + e.InnerException);
                write.WriteLine("Extra Data: \n");

                if (e.Data != null)
                {
                    foreach (DictionaryEntry b in e.Data)
                    {
                        write.WriteLine(b.Key + " : " + b.Value);
                    }
                }

                write.Close();
            }
            finally
            {
            }
#endif
        }
    }
#endif
}
