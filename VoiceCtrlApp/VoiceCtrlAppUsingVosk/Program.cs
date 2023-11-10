using System;
using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using NAudio.Wave;
using Vosk;

class Program
{
    /// <summary>M5のIPアドレス</summary>
    private string _ipAddress = "192.168.10.115";

    /// <summary>M5 UDPポート番号</summary>
    private int _udpPortNo = 12345;

    /// <summary>モーター速度レベル</summary>
    private int motorSpeedLevel = 0;

    /// <summary>vosk モデル</summary>
    /// <remarks>
    /// modelを下記からダウンロード 展開してパスを指定
    /// https://alphacephei.com/vosk/models
    /// </remarks>
    private string _modelPath = @"..\vosk-model-ja-0.22";

    /// <summary>
    /// レーベンシュタイン距離
    /// </summary>
    /// <param name="str1"></param>
    /// <param name="str2"></param>
    /// <returns></returns>
    public int LevenshteinDistance(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1))
        {
            return string.IsNullOrEmpty(str2) ? 0 : str2.Length;
        }

        if (string.IsNullOrEmpty(str2))
        {
            return str1.Length;
        }

        int lengthS = str1.Length;
        int lengthT = str2.Length;
        var distances = new int[lengthS + 1, lengthT + 1];

        for (int i = 0; i <= lengthS; distances[i, 0] = i++) ;
        for (int j = 0; j <= lengthT; distances[0, j] = j++) ;

        for (int i = 1; i <= lengthS; i++)
        {
            for (int j = 1; j <= lengthT; j++)
            {
                int cost = (str2[j - 1] == str1[i - 1]) ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }
        return distances[lengthS, lengthT];
    }

    /// <summary>
    /// 類似度
    /// </summary>
    /// <param name="str1"></param>
    /// <param name="str2"></param>
    /// <returns></returns>
    private double CalculateSimilarity(string str1, string str2)
    {
        int distance = LevenshteinDistance(str1, str2);
        int maxLength = Math.Max(str1.Length, str2.Length);
        return 1.0 - ((double)distance / maxLength);
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
    /// 音声認識で制御
    /// </summary>
    public void StartVoiceCtrl()
    {
        // Voskモデル設定
        var modelPath = System.IO.Path.GetFullPath(_modelPath);
        var model = new Model(modelPath);
        var recognizer = new VoskRecognizer(model, 16000f);

        // 音声認識
        var waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(16000, 1);
        waveIn.DataAvailable += (sender, e) =>
        {
            if (recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                // 認識イベント JSONをパースして認識文字を抽出
                string jsonString = recognizer.Result();
                JsonDocument doc = JsonDocument.Parse(jsonString);
                var recogText = doc.RootElement.GetProperty("text").GetString();
                recogText = recogText?.Replace(" ", "");

                // 認識文字 表示
                Console.WriteLine(recogText);

                // 認識文字 モーター速度制御
                double SimilarityThd = 0.2;
                if (CalculateSimilarity(recogText, "加速") > SimilarityThd)
                {
                    Console.WriteLine($"加速 {CalculateSimilarity(recogText, "加速")}");
                    motorSpeedLevel++;
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
                    SendUDPByte(val);
                }
                else if (CalculateSimilarity(recogText, "セイバーゴー") > SimilarityThd)
                {
                    Console.WriteLine($"セイバーゴー {CalculateSimilarity(recogText, "セイバーゴー")}");
                    SendUDPByte('5');
                }
                else if (CalculateSimilarity(recogText, "スタート") > SimilarityThd)
                {
                    Console.WriteLine($"スタート {CalculateSimilarity(recogText, "スタート")}");
                    SendUDPByte('4');
                }
                else if (CalculateSimilarity(recogText, "ストップ") > SimilarityThd)
                {
                    Console.WriteLine($"ストップ {CalculateSimilarity(recogText, "ストップ")}");
                    SendUDPByte('0');
                }
                else if (CalculateSimilarity(recogText, "いけぇ") > SimilarityThd)
                {
                    Console.WriteLine($"いけぇ {CalculateSimilarity(recogText, "いけぇ")}");
                    SendUDPByte('3');
                }
                else if (CalculateSimilarity(recogText, "どうしたんだマグナム") > SimilarityThd)
                {
                    Console.WriteLine($"どうしたんだマグナム {CalculateSimilarity(recogText, "どうしたんだマグナム")}");
                    SendUDPByte('4');
                }
                else if (CalculateSimilarity(recogText, "がんばれマグナム") > SimilarityThd)
                {
                    Console.WriteLine($"がんばれマグナム {CalculateSimilarity(recogText, "がんばれマグナム")}");
                    SendUDPByte('5');
                }
                else if (CalculateSimilarity(recogText, "しっかりしろマグナム") > SimilarityThd)
                {
                    Console.WriteLine($"しっかりしろマグナム {CalculateSimilarity(recogText, "しっかりしろマグナム")}");
                    SendUDPByte('6');
                }
                else if (CalculateSimilarity(recogText, "かっとべマグナム") > SimilarityThd)
                {
                    Console.WriteLine($"かっとべマグナム {CalculateSimilarity(recogText, "しっかりしろマグナム")}");
                    SendUDPByte('9');
                }
            }
        };

        // 音声認識の開始
        waveIn.StartRecording();
        Console.WriteLine("音声認識開始 Enterで終了");
        Console.ReadLine();

        // 音声認識の停止
        waveIn.StopRecording();
        recognizer.Dispose();
        model.Dispose();
    }

    /// <summary>
    /// Main
    /// </summary>
    static void Main()
    {
        var p = new Program();
        p.StartVoiceCtrl();
    }
}
