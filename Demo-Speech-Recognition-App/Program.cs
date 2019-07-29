using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Threading.Tasks;

namespace Demo_Speech_Recognition_App
{
    public class Program
    {
        static void Main(string[] args)
        {
            var prompt = "Your choice (0: Stop): ";
            Console.WriteLine("1. Speech recognition with microphone input.");
            Console.WriteLine("2. Speech continuous recognition with microphone input.");
            Console.Write(prompt);

            ConsoleKeyInfo x;
            do
            {
                x = Console.ReadKey();
                Console.WriteLine("");
                switch (x.Key)
                {
                    case ConsoleKey.D1:
                        RecognitionWithMicrophoneAsync().Wait();
                        break;
                    case ConsoleKey.D2:
                        ContinuousRecognitionWithMicrophoneAsync().Wait();
                        break;
                    case ConsoleKey.D0:
                        Console.WriteLine("Exiting...");
                        break;
                    default:
                        Console.WriteLine("Invalid input.");
                        break;
                }
                Console.WriteLine("\nExecution done. " + prompt);
            } while (x.Key != ConsoleKey.D0);
        }

        // 可以從下面的網址申請試用認知服務
        // https://azure.microsoft.com/zh-tw/try/cognitive-services/my-apis/
        // 完成申請後，將 YOUR_SUBSCRIPTION_KEY 變更成你的訂閱金鑰及地區
        private const string YourSubscriptionKey = "YOUR_SUBSCRIPTION_KEY";
        private const string YourServiceRegion = "westus";
        private const string YourEndpointId = "https://westus.api.cognitive.microsoft.com/sts/v1.0";

        // 使用麥克風進行語音辨識
        public static async Task RecognitionWithMicrophoneAsync()
        {
            // 建立語音辨識的設定，這裡必須提供 Azure Cognitive Service 的訂閱金鑰和服務區域
            var config = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
            // 預設使用 en-us 的美式英文作為辨識語言
            config.SpeechRecognitionLanguage = "en-us";

            // 建立語音辨識器，並將音訊來源指定為機器預設的麥克風
            using (var recognizer = new SpeechRecognizer(config, AudioConfig.FromDefaultMicrophoneInput()))
            {
                Console.WriteLine("Say something...");

                // 開始進行語音辨識，會在辨別出句子結束時，返回語音辨識的結果。
                // 會藉由句子說完後，所產生的靜默時間作為辨識依據，或者語音超過 15 秒，也會處理成斷句。
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                // 輸出語音辨識結果
                switch (result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        Console.WriteLine($"RECOGNIZED: {result.Text}");
                        break;
                    case ResultReason.NoMatch:
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        break;
                    case ResultReason.Canceled:
                    default:
                        var cancellation = CancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                        break;
                }
            }
        }

        // 使用麥克風持續進行語音辨識
        public static async Task ContinuousRecognitionWithMicrophoneAsync()
        {
            // 建立語音辨識的設定，這裡必須提供 Azure Cognitive Service 的訂閱金鑰和服務區域
            var config = SpeechConfig.FromSubscription(YourSubscriptionKey, YourServiceRegion);
            // 預設使用 en-us 的美式英文作為辨識語言
            config.SpeechRecognitionLanguage = "en-us";

            var silenceCount = 0;
            var stopRecognition = new TaskCompletionSource<int>();

            // 建立語音辨識器，並將音訊來源指定為機器預設的麥克風
            using (var recognizer = new SpeechRecognizer(config, AudioConfig.FromDefaultMicrophoneInput()))
            {
                Console.WriteLine("Say something...");

                // 設定語音辨識完成後，會進行的動作
                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED: {e.Result.Text}");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }

                    if (silenceCount == 3) stopRecognition.TrySetResult(0);
                    if (e.Result.Text.ToLower().StartsWith("stop")) stopRecognition.TrySetResult(0);
                };

                // 設定語音辨識器收到 Cancel 訊號的時候，會進行的動作
                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }

                    stopRecognition.TrySetResult(0);
                };

                recognizer.SessionStarted += (s, e) =>
                {
                    Console.WriteLine("\n    Session started event.");
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\n    Session stopped event.");
                    Console.WriteLine("\nStop recognition.");
                    stopRecognition.TrySetResult(0);
                };

                // 開始進行語音辨識
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                // Waits for completion.
                // Use Task.WaitAny to keep the task rooted.
                Task.WaitAny(new[] { stopRecognition.Task });

                // 結束語音辨識
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
        }
    }
}
