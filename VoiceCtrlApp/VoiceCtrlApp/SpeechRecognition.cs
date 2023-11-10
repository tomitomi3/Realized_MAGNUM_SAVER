using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace VoiceCtrlApp
{
    /// <summary>
    /// 音声認識クラス
    /// </summary>
    public static class SpeechRecognition
    {
        public static SpeechRecognitionEngine Engine;

        public static bool IsAvailable
        {
            get { return (Engine != null && !IsDestroyed); }
        }

        public static bool IsRecognizing
        {
            get { return (IsAvailable && Engine.AudioState != AudioState.Stopped); }
        }

        public static System.Collections.ObjectModel.ReadOnlyCollection<RecognizerInfo> InstalledRecognizers
        {
            get { return SpeechRecognitionEngine.InstalledRecognizers(); }
        }

        //https://learn.microsoft.com/ja-jp/dotnet/api/system.speech.recognition.speechhypothesizedeventargs?view=netframework-4.8.1
        //認識エンジンが入力フレーズの識別を試みると、多数 SpeechHypothesized のイベントが生成されます。 通常、これらのイベントの処理はデバッグにのみ役立ちます。
        //public static System.Action<SpeechHypothesizedEventArgs> SpeechHypothesizedEvent;

        public static System.Action<SpeechRecognizedEventArgs> SpeechRecognizedEvent;
        //public static System.Action<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejectedEvent;
        public static System.Action<RecognizeCompletedEventArgs> SpeechRecognizeCompletedEvent;

        private static bool IsDestroyed;

        static SpeechRecognition()
        {
            IsDestroyed = true;
        }

        public static void DestroyEngine()
        {
            if (!IsAvailable) { return; }

            //Engine.SpeechHypothesized -= SpeechHypothesized;
            Engine.SpeechRecognized -= SpeechRecognized;
            //Engine.SpeechRecognitionRejected -= SpeechRecognitionRejected;
            Engine.RecognizeCompleted -= SpeechRecognizeCompleted;
            Engine.UnloadAllGrammars();
            Engine.Dispose();

            IsDestroyed = true;
        }

        public static void AddGrammar(string grammarName, params string[] words)
        {
            Choices choices = new Choices();
            choices.Add(words);

            GrammarBuilder grammarBuilder = new GrammarBuilder();
            grammarBuilder.Append(choices);

            Grammar grammar = new Grammar(grammarBuilder)
            {
                Name = grammarName
            };

            if (!IsAvailable) { return; }

            Engine.LoadGrammar(grammar);
        }

        public static void ClearGrammar()
        {
            if (!IsAvailable) { return; }

            Engine.UnloadAllGrammars();
        }

        public static void RecognizeAsync(bool multiple)
        {
            if (IsRecognizing || Engine.Grammars.Count <= 0)
            {
                return;
            }

            RecognizeMode mode = (multiple) ? RecognizeMode.Multiple : RecognizeMode.Single;
            Engine.RecognizeAsync(mode);
        }

        public static void RecognizeAsyncCancel()
        {
            if (!IsRecognizing) { return; }

            Engine.RecognizeAsyncCancel();
        }

        public static void RecognizeAsyncStop()
        {
            if (!IsRecognizing) { return; }

            Engine.RecognizeAsyncStop();
        }

        public static void CreateEngine()
        {
            if (IsAvailable) { return; }

            Engine = new SpeechRecognitionEngine();

            IsDestroyed = false;

            Engine.SetInputToDefaultAudioDevice();

            //Engine.SpeechHypothesized += SpeechHypothesized;
            Engine.SpeechRecognized += SpeechRecognized;
            //Engine.SpeechRecognitionRejected += SpeechRecognitionRejected;
            Engine.RecognizeCompleted += SpeechRecognizeCompleted;
        }

        private static void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null && SpeechRecognizedEvent != null)
            {
                SpeechRecognizedEvent(e);
            }
        }

        private static void SpeechRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Result != null && SpeechRecognizeCompletedEvent != null)
            {
                SpeechRecognizeCompletedEvent(e);
            }
        }
    }
}
