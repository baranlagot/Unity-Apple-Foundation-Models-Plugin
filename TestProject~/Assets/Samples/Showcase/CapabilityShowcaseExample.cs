using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    /// <summary>
    /// A self-contained, understandable on-screen showcase of every Apple Foundation
    /// Models capability. It uses IMGUI so it renders reliably on device without any UI
    /// theme, prefab, or font asset. Each capability has a labeled button and its result
    /// is written to the live log.
    /// </summary>
    public sealed class CapabilityShowcaseExample : MonoBehaviour
    {
        [Serializable]
        private sealed class QuestData
        {
            public string title;
            public string objective;
            public int rewardCoins;
            public string npcName;
        }

        private IAppleFoundationModelsClient _client;
        private readonly StringBuilder _log = new StringBuilder();
        private Texture2D _background;

        private string _prompt = "Write a short, cheerful greeting for a Unity developer.";
        private string _availability = "Not checked yet.";
        private string _streamingOutput = string.Empty;
        private bool _busy;
        private bool _streaming;
        private CancellationTokenSource _streamCts;
        private Vector2 _scroll;

        private GUIStyle _title;
        private GUIStyle _subtitle;
        private GUIStyle _section;
        private GUIStyle _body;
        private GUIStyle _button;
        private GUIStyle _field;
        private GUIStyle _logStyle;
        private bool _stylesReady;

        private void Awake()
        {
            _client = AppleFoundationModels.DefaultClient;
            Application.targetFrameRate = 60;

            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0.09f, 0.10f, 0.13f, 1f));
            _background.Apply();
        }

        private async void Start()
        {
            Append("Apple Foundation Models capability showcase ready.");
            Append("In the Editor a deterministic mock responds. On device you get real");
            Append("on-device results, or a clear unavailable status if the device is not eligible.");
            await CheckAvailabilityAsync();
        }

        private void OnDestroy()
        {
            _streamCts?.Cancel();
            _streamCts?.Dispose();
            _streamCts = null;
            if (_background != null)
            {
                Destroy(_background);
            }
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            _title.normal.textColor = new Color(0.95f, 0.97f, 1f);

            _subtitle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            _subtitle.normal.textColor = new Color(0.75f, 0.80f, 0.88f);

            _section = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            _section.normal.textColor = new Color(0.55f, 0.80f, 1f);

            _body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            _body.normal.textColor = new Color(0.90f, 0.92f, 0.95f);

            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fixedHeight = 46,
                margin = new RectOffset(0, 0, 4, 4)
            };

            _field = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                wordWrap = true,
                padding = new RectOffset(8, 8, 8, 8)
            };

            _logStyle = new GUIStyle(GUI.skin.textArea) { fontSize = 13, wordWrap = true };
            _logStyle.normal.textColor = new Color(0.85f, 0.90f, 0.85f);

            _stylesReady = true;
        }

        private void OnGUI()
        {
            // Solid backdrop so text is always legible regardless of the camera clear.
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _background);

            EnsureStyles();

            // Lay out in a fixed virtual width so sizes are consistent across screen DPIs.
            const float referenceWidth = 430f;
            var scale = Screen.width / referenceWidth;
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var viewWidth = referenceWidth;
            var viewHeight = Screen.height / scale;

            GUILayout.BeginArea(new Rect(14, 14, viewWidth - 28, viewHeight - 28));
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Apple Foundation Models", _title);
            GUILayout.Label("Unity package capability showcase", _subtitle);
            GUILayout.Space(10);

            GUILayout.Label("Availability", _section);
            GUILayout.Label(_availability, _body);
            if (Button("Check Availability"))
            {
                _ = CheckAvailabilityAsync();
            }
            GUILayout.Space(10);

            GUILayout.Label("Prompt", _section);
            GUILayout.Label("Used by Generate Text and Streaming below.", _subtitle);
            _prompt = GUILayout.TextArea(_prompt, _field, GUILayout.MinHeight(64));
            GUILayout.Space(10);

            GUILayout.Label("Text generation", _section);
            GUILayout.Label("One-shot prompt to a full text response.", _subtitle);
            if (Button("Generate Text"))
            {
                _ = GenerateTextAsync();
            }
            GUILayout.Space(10);

            GUILayout.Label("Streaming", _section);
            GUILayout.Label("Streams the response token by token.", _subtitle);
            if (!string.IsNullOrEmpty(_streamingOutput))
            {
                GUILayout.Label(_streamingOutput, _body);
            }
            GUILayout.BeginHorizontal();
            if (Button("Stream Text"))
            {
                _ = StreamTextAsync();
            }
            GUI.enabled = _streaming;
            if (Button("Cancel Stream"))
            {
                _streamCts?.Cancel();
                Append("Cancellation requested.");
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Label("Structured JSON", _section);
            GUILayout.Label("Generates JSON and parses it into a C# object.", _subtitle);
            if (Button("Generate JSON Quest"))
            {
                _ = GenerateJsonAsync();
            }
            GUILayout.Space(10);

            GUILayout.Label("Full device validation", _section);
            GUILayout.Label("Runs every scenario and prints a privacy-safe report.", _subtitle);
            if (Button("Run Full Validation"))
            {
                _ = RunFullValidationAsync();
            }
            GUILayout.Space(14);

            GUILayout.Label("Output log", _section);
            if (Button("Clear Log"))
            {
                _log.Clear();
            }
            GUILayout.TextArea(_log.ToString(), _logStyle, GUILayout.MinHeight(180));

            GUILayout.Space(20);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.matrix = previousMatrix;
        }

        private bool Button(string label)
        {
            // One request at a time keeps the log readable; streaming manages its own state.
            var wasEnabled = GUI.enabled;
            if (_busy)
            {
                GUI.enabled = false;
            }

            var clicked = GUILayout.Button(label, _button);
            GUI.enabled = wasEnabled;
            return clicked;
        }

        private async Task CheckAvailabilityAsync()
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            try
            {
                var availability = await _client.GetAvailabilityAsync();
                _availability = availability.Status + " — " + availability.Message;
                Append("Availability: " + _availability);
            }
            catch (Exception exception)
            {
                _availability = "Error: " + exception.Message;
                Append("Availability error: " + exception.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task GenerateTextAsync()
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            Append("Generating text for: \"" + Trim(_prompt) + "\"");
            try
            {
                var result = await _client.GenerateTextAsync(_prompt);
                Append(result.IsSuccess
                    ? "Text result: " + result.Text
                    : "Text failed: " + result.ErrorMessage);
            }
            catch (Exception exception)
            {
                Append("Text error: " + exception.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task StreamTextAsync()
        {
            if (_busy || _streaming)
            {
                return;
            }

            _streaming = true;
            _busy = true;
            _streamingOutput = string.Empty;
            _streamCts = new CancellationTokenSource();
            Append("Streaming for: \"" + Trim(_prompt) + "\"");

            try
            {
                await _client.StreamTextAsync(
                    _prompt,
                    token => _streamingOutput += token,
                    result => Append("Streaming complete (" + result.Text.Length + " chars)."),
                    error => Append("Streaming error: " + error.Message),
                    cancellationToken: _streamCts.Token);
            }
            catch (OperationCanceledException)
            {
                Append("Streaming cancelled.");
            }
            catch (Exception exception)
            {
                Append("Streaming error: " + exception.Message);
            }
            finally
            {
                _streaming = false;
                _busy = false;
                _streamCts?.Dispose();
                _streamCts = null;
            }
        }

        private async Task GenerateJsonAsync()
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            Append("Generating structured JSON quest...");
            try
            {
                var quest = await _client.GenerateJsonAsync<QuestData>(
                    "Generate a cozy quest with fields title, objective, rewardCoins, and npcName.");
                Append("JSON parsed -> title: '" + quest.title + "', objective: '" + quest.objective +
                       "', reward: " + quest.rewardCoins + ", npc: '" + quest.npcName + "'");
            }
            catch (Exception exception)
            {
                Append("JSON error: " + exception.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task RunFullValidationAsync()
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            Append("Running full device validation...");
            try
            {
                var runner = new DeviceValidationRunner(
                    _client,
                    new DefaultDeviceValidationEnvironment());
                var report = await runner.RunAsync(CancellationToken.None);
                Append(report.ToDisplayText());
            }
            catch (Exception exception)
            {
                Append("Validation error: " + exception.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        private static string Trim(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= 60 ? value : value.Substring(0, 57) + "...";
        }

        private void Append(string line)
        {
            _log.AppendLine(line);
            _scroll.y = float.MaxValue;
        }
    }
}
