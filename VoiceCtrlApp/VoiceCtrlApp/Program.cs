using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace VoiceCtrlApp
{
    /// <summary>
    /// メイン
    /// </summary>
    /// <remarks>
    /// 参考
    /// [1]C#でWindows標準の音声認識で遊んでみた, https://qiita.com/kob58im/items/9069dcacd3d2cb867a21
    /// </remarks>
    public class Program : IDisposable
    {
        Dictionary<string, Action> myActions = new Dictionary<string, Action>();

        private bool disposedValue;

        /// <summary>M5のIPアドレス</summary>
        private string _ipAddress = "192.168.10.115";

        /// <summary>M5 UDPポート番号</summary>
        private int _udpPortNo = 12345;

        /// <summary>モーター速度レベル</summary>
        private int motorSpeedLevel = 0;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public Program()
        {
            Init();
            AddGrammar();
            SpeechRecognition.RecognizeAsync(true); // falseにすると、一回認識すると終了する
        }

        /// <summary>
        /// モーター制御 UDPパケットを送信
        /// </summary>
        /// <param name="motorSpeedLevelAscii">モーター速度 Asciiコード</param>
        private void SendUDPByte(char motorSpeedLevelAscii)
        {
            using (var udpClient = new UdpClient())
            {
                var remoteEndpoint = new IPEndPoint(IPAddress.Parse(this._ipAddress), this._udpPortNo);

                var sendData = new byte[1];
                sendData[0] = Convert.ToByte(motorSpeedLevelAscii);

                udpClient.Send(sendData, sendData.Length, remoteEndpoint);
                udpClient.Close();
            }
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Init()
        {
            myActions.Add("ストップ", () =>
            {
                this.SendUDPByte('0');
                Console.WriteLine("Stop");
            });

            myActions.Add("スタート", () =>
            {
                this.SendUDPByte('1');
                Console.WriteLine("Start");
            });

            myActions.Add("加速", () =>
            {
                motorSpeedLevel++;
                if (motorSpeedLevel < 0)
                {
                    motorSpeedLevel = 0;
                }
                if (motorSpeedLevel > 8)
                {
                    motorSpeedLevel = 7;    //MAX
                }
                char val = (char)('0' + motorSpeedLevel);
                this.SendUDPByte(val);
                Console.WriteLine($"Accel Level : {motorSpeedLevel}");
            });

            myActions.Add("いけぇ", () =>
            {
                this.SendUDPByte('2');
                Console.WriteLine("いけぇの反応");
            });
            myActions.Add("ごぉぉぉ", () =>
            {
                this.SendUDPByte('7');
                Console.WriteLine("ごぉぉぉの反応");
            });

            myActions.Add("どうしたんだマグナム", () =>
            {
                this.SendUDPByte('3');
                Console.WriteLine("どうしたんだマグナム");
            });

            myActions.Add("がんばれマグナム", () =>
            {
                this.SendUDPByte('5');
                Console.WriteLine("がんばれマグナム");
            });

            myActions.Add("しっかりしろマグナム", () =>
            {
                this.SendUDPByte('5');
                Console.WriteLine("しっかりしろマグナム");
            });

            myActions.Add("かっとべマグナム", () =>
            {
                this.SendUDPByte('7');
                Console.WriteLine("かっとべマグナム");
            });

            SpeechRecognition.CreateEngine();

            foreach (RecognizerInfo ri in SpeechRecognition.InstalledRecognizers)
            {
                Console.WriteLine(ri.Name + "(" + ri.Culture + ")");
            }

            SpeechRecognition.SpeechRecognizedEvent = (e) =>
            {
                // 信頼度低い場合はスルー
                Console.WriteLine($"Confidence : {e.Result.Confidence}");
                if (e.Result.Confidence < 0.4) return;

                Console.WriteLine("確定：" + e.Result.Grammar.Name + " " + e.Result.Text + "(" + e.Result.Confidence + ")");

                if (myActions.ContainsKey(e.Result.Text))
                {
                    Action act = myActions[e.Result.Text];
                    act();
                }
            };

            SpeechRecognition.SpeechRecognizeCompletedEvent = (e) =>
            {
                if (e.Cancelled)
                {
                    Console.WriteLine("キャンセルされました。");
                }

                Console.WriteLine("認識終了");
            };
        }

        private void AddGrammar()
        {
            var tmp = myActions.Keys;
            string[] words = new string[tmp.Count];
            tmp.CopyTo(words, 0);
            SpeechRecognition.AddGrammar("words", words);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 終了時に開放する
                    //SpeechRecognition.ClearGrammar();
                    SpeechRecognition.RecognizeAsyncCancel();
                    SpeechRecognition.RecognizeAsyncStop();
                    SpeechRecognition.DestroyEngine();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~Program()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// メイン
        /// </summary>
        public static void Main()
        {
            // 音声認識が始まる
            var recognize = new Program();

            Console.WriteLine("音声認識を停止するには何かキーを押してください...");
            Console.ReadKey();  // ユーザーの入力を待機
            recognize.Dispose();

        }
    }
}
