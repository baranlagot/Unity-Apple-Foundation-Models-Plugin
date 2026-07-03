using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Baran.AppleFoundationModels.ImagePlayground;
using Baran.AppleFoundationModels.Vision;
using UnityEngine;

namespace Baran.AppleFoundationModels.Samples
{
    /// <summary>
    /// A self-contained, understandable on-screen showcase of the Apple Foundation Models
    /// package. It uses IMGUI so it renders reliably on device and Simulator without any UI
    /// theme, prefab, or font asset. Each capability has a clearly labeled control and its
    /// result streams into a prominent output panel.
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

        private static readonly (string Label, string Prompt)[] Presets =
        {
            ("Haiku", "Write a haiku about the ocean."),
            ("Explain", "Explain how rainbows form, in two sentences."),
            ("Story", "Tell a one-paragraph bedtime story about a brave little robot."),
            ("Facts", "List five fun facts about octopuses.")
        };

        private static readonly (string Label, int Value)[] LengthPresets =
        {
            ("Short", 64),
            ("Medium", 256),
            ("Long", 0)
        };

        private IAppleFoundationModelsClient _client;
        private Texture2D _background;
        private Texture2D _generatedImage;

        private string _prompt = "Write a short, cheerful greeting for a Unity developer.";
        private string _instructions = string.Empty;
        private float _temperature = 0.7f;
        private int _maxTokens;

        private string _availabilityText = "Checking...";
        private SampleTone _availabilityTone = SampleTone.Neutral;
        private string _status = "Ready.";
        private SampleTone _statusTone = SampleTone.Neutral;
        private string _output = "Results appear here.";

        private readonly StringBuilder _log = new StringBuilder();
        private bool _busy;
        private bool _streaming;
        private CancellationTokenSource _streamCts;
        private Vector2 _scroll;

        private GUIStyle _title;
        private GUIStyle _pill;
        private GUIStyle _section;
        private GUIStyle _hint;
        private GUIStyle _button;
        private GUIStyle _smallButton;
        private GUIStyle _field;
        private GUIStyle _log_;
        private bool _stylesReady;

        private enum SampleTone
        {
            Neutral,
            Working,
            Success,
            Warning,
            Error
        }

        private void Awake()
        {
            _client = AppleFoundationModels.DefaultClient;
            Application.targetFrameRate = 60;

            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.12f, 1f));
            _background.Apply();
        }

        private async void Start()
        {
            Append("Apple Foundation Models capability showcase started.");
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
            if (_generatedImage != null)
            {
                Destroy(_generatedImage);
            }
        }

        // ----- UI ---------------------------------------------------------------

        private void OnGUI()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _background);
            EnsureStyles();

            const float referenceWidth = 440f;
            var scale = Screen.width / referenceWidth;
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            var viewWidth = referenceWidth;
            var viewHeight = Screen.height / scale;

            GUILayout.BeginArea(new Rect(14, 14, viewWidth - 28, viewHeight - 28));
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Apple Foundation Models", _title);
            DrawPill("Availability: " + _availabilityText, _availabilityTone);
            GUILayout.Space(8);

            // Output panel — always visible, shows the current result / live stream.
            GUILayout.Label("Output", _section);
            DrawPill(_status, _statusTone);
            DrawOutputPanel();
            GUILayout.Space(12);

            // Prompt + presets.
            GUILayout.Label("Prompt", _section);
            _prompt = GUILayout.TextArea(_prompt, _field, GUILayout.MinHeight(58));
            GUILayout.BeginHorizontal();
            foreach (var preset in Presets)
            {
                if (GUILayout.Button(preset.Label, _smallButton))
                {
                    _prompt = preset.Prompt;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(12);

            // Options: instructions, temperature, length.
            GUILayout.Label("Options", _section);
            GUILayout.Label("System instructions (steer the model's behaviour)", _hint);
            _instructions = GUILayout.TextArea(_instructions, _field, GUILayout.MinHeight(44));
            GUILayout.Label(
                "Temperature: " + _temperature.ToString("0.00") + "  (0 = focused, 1 = creative)",
                _hint);
            _temperature = GUILayout.HorizontalSlider(_temperature, 0f, 1f);
            GUILayout.Label("Max length", _hint);
            GUILayout.BeginHorizontal();
            foreach (var length in LengthPresets)
            {
                var selected = _maxTokens == length.Value;
                var label = (selected ? "● " : "") + length.Label;
                if (GUILayout.Button(label, _smallButton))
                {
                    _maxTokens = length.Value;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(14);

            // Capabilities.
            GUILayout.Label("Capabilities", _section);
            if (Button("Check Availability"))
            {
                _ = CheckAvailabilityAsync();
            }
            if (Button("Generate Text"))
            {
                _ = GenerateTextAsync();
            }
            GUILayout.BeginHorizontal();
            if (Button("Stream Text"))
            {
                _ = StreamTextAsync();
            }
            GUI.enabled = _streaming;
            if (GUILayout.Button("Stop", _button))
            {
                _streamCts?.Cancel();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            if (Button("Generate JSON Quest"))
            {
                _ = GenerateJsonAsync();
            }
            if (Button("Run Full Validation"))
            {
                _ = RunFullValidationAsync();
            }
            GUILayout.Space(14);

            // Vision — on-device image understanding (no Apple Intelligence required).
            GUILayout.Label("Vision (image understanding)", _section);
            GUILayout.Label(
                "Captures the current screen and analyses it on-device with the Vision framework.",
                _hint);
            GUILayout.BeginHorizontal();
            if (Button("Classify Screen"))
            {
                CaptureAndAnalyze(AppleVisionRequestKind.Classify);
            }
            if (Button("Read Text (OCR)"))
            {
                CaptureAndAnalyze(AppleVisionRequestKind.RecognizeText);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(14);

            // Image Playground — on-device image generation (needs Apple Intelligence).
            GUILayout.Label("Image generation (Image Playground)", _section);
            GUILayout.Label(
                "Generates an image from the prompt above. Requires an Apple Intelligence device.",
                _hint);
            if (Button("Generate Image"))
            {
                _ = GenerateImageAsync();
            }
            if (_generatedImage != null)
            {
                var rect = GUILayoutUtility.GetRect(220f, 220f);
                GUI.DrawTexture(rect, _generatedImage, ScaleMode.ScaleToFit);
            }
            GUILayout.Space(14);

            // Event log.
            GUILayout.BeginHorizontal();
            GUILayout.Label("Event log", _section);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear", _smallButton))
            {
                _log.Clear();
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(_log.ToString(), _log_);

            GUILayout.Space(24);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.matrix = previous;
        }

        private void DrawOutputPanel()
        {
            var box = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                fontSize = 15,
                padding = new RectOffset(12, 12, 12, 12)
            };
            box.normal.textColor = new Color(0.92f, 0.95f, 0.98f);
            GUILayout.Label(
                string.IsNullOrEmpty(_output) ? " " : _output,
                box,
                GUILayout.MinHeight(120));
        }

        private void DrawPill(string text, SampleTone tone)
        {
            var previous = GUI.color;
            GUI.color = ToneColor(tone);
            GUILayout.Label(text, _pill);
            GUI.color = previous;
        }

        private static Color ToneColor(SampleTone tone)
        {
            switch (tone)
            {
                case SampleTone.Working: return new Color(0.36f, 0.60f, 0.95f);
                case SampleTone.Success: return new Color(0.35f, 0.75f, 0.45f);
                case SampleTone.Warning: return new Color(0.90f, 0.70f, 0.25f);
                case SampleTone.Error: return new Color(0.90f, 0.38f, 0.38f);
                default: return new Color(0.45f, 0.48f, 0.55f);
            }
        }

        private bool Button(string label)
        {
            var wasEnabled = GUI.enabled;
            if (_busy)
            {
                GUI.enabled = false;
            }

            var clicked = GUILayout.Button(label, _button);
            GUI.enabled = wasEnabled;
            return clicked;
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            _title.normal.textColor = new Color(0.96f, 0.98f, 1f);

            _pill = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                padding = new RectOffset(10, 10, 6, 6)
            };
            _pill.normal.textColor = Color.white;

            _section = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            _section.normal.textColor = new Color(0.60f, 0.82f, 1f);

            _hint = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            _hint.normal.textColor = new Color(0.68f, 0.72f, 0.80f);

            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fixedHeight = 48,
                margin = new RectOffset(0, 0, 4, 4)
            };

            _smallButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fixedHeight = 38,
                margin = new RectOffset(2, 2, 2, 2)
            };

            _field = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                wordWrap = true,
                padding = new RectOffset(8, 8, 8, 8)
            };

            _log_ = new GUIStyle(GUI.skin.textArea) { fontSize = 12, wordWrap = true };
            _log_.normal.textColor = new Color(0.80f, 0.86f, 0.80f);

            _stylesReady = true;
        }

        // ----- Capability actions ----------------------------------------------

        private AppleFoundationModelsOptions BuildOptions()
        {
            return new AppleFoundationModelsOptions
            {
                Instructions = string.IsNullOrWhiteSpace(_instructions) ? null : _instructions,
                Temperature = _temperature,
                MaxOutputTokens = _maxTokens > 0 ? _maxTokens : (int?)null
            };
        }

        private async Task CheckAvailabilityAsync()
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            SetStatus("Checking availability...", SampleTone.Working);
            try
            {
                var availability = await _client.GetAvailabilityAsync();
                _availabilityText = availability.Status.ToString();
                _availabilityTone = availability.IsAvailable ? SampleTone.Success : SampleTone.Warning;
                SetStatus("Availability checked.", _availabilityTone);
                Append("Availability: " + availability.Status + " - " + availability.Message);
            }
            catch (Exception exception)
            {
                _availabilityText = "Error";
                _availabilityTone = SampleTone.Error;
                Fail("Availability", exception);
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
            SetStatus("Generating...", SampleTone.Working);
            _output = string.Empty;
            Append("Generate: \"" + Trim(_prompt) + "\"");
            try
            {
                var result = await _client.GenerateTextAsync(_prompt, BuildOptions());
                if (result.IsSuccess)
                {
                    _output = result.Text;
                    SetStatus("Done (" + result.Text.Length + " chars).", SampleTone.Success);
                }
                else
                {
                    _output = result.ErrorMessage;
                    SetStatus("Provider reported a failure.", SampleTone.Error);
                }
            }
            catch (Exception exception)
            {
                Fail("Generate", exception);
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
            _output = string.Empty;
            _streamCts = new CancellationTokenSource();
            SetStatus("Streaming...", SampleTone.Working);
            Append("Stream: \"" + Trim(_prompt) + "\"");

            try
            {
                await _client.StreamTextAsync(
                    _prompt,
                    token => _output += token,
                    result => SetStatus("Stream complete.", SampleTone.Success),
                    error => Fail("Stream", error),
                    BuildOptions(),
                    _streamCts.Token);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Stream cancelled.", SampleTone.Warning);
                Append("Stream cancelled.");
            }
            catch (Exception exception)
            {
                Fail("Stream", exception);
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
            SetStatus("Generating structured JSON...", SampleTone.Working);
            _output = string.Empty;
            Append("Generate JSON quest.");
            try
            {
                var quest = await _client.GenerateJsonAsync<QuestData>(
                    "Generate a cozy quest with fields title, objective, rewardCoins, and npcName.");
                _output =
                    "Title: " + quest.title + "\n" +
                    "Objective: " + quest.objective + "\n" +
                    "Reward: " + quest.rewardCoins + " coins\n" +
                    "NPC: " + quest.npcName;
                SetStatus("Parsed JSON into a C# object.", SampleTone.Success);
            }
            catch (Exception exception)
            {
                Fail("JSON", exception);
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
            SetStatus("Running full validation...", SampleTone.Working);
            _output = "Running every scenario...";
            Append("Full validation started.");
            try
            {
                var runner = new DeviceValidationRunner(
                    _client,
                    new DefaultDeviceValidationEnvironment());
                var report = await runner.RunAsync(CancellationToken.None);
                _output = report.ToDisplayText();
                SetStatus("Validation complete.", SampleTone.Success);
            }
            catch (Exception exception)
            {
                Fail("Validation", exception);
            }
            finally
            {
                _busy = false;
            }
        }

        private void CaptureAndAnalyze(AppleVisionRequestKind kind)
        {
            if (_busy)
            {
                return;
            }

            if (!AppleVision.IsSupported)
            {
                SetStatus("Vision runs on an iOS device or Simulator.", SampleTone.Warning);
                Append("Vision is only available in an iOS player.");
                return;
            }

            StartCoroutine(CaptureRoutine(kind));
        }

        private IEnumerator CaptureRoutine(AppleVisionRequestKind kind)
        {
            _busy = true;
            SetStatus("Capturing screen...", SampleTone.Working);
            // The screenshot must be grabbed after the frame has finished rendering.
            yield return new WaitForEndOfFrame();

            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] png;
            try
            {
                png = texture.EncodeToPNG();
            }
            finally
            {
                Destroy(texture);
            }

            _ = RunVisionAsync(kind, png);
        }

        private async Task GenerateImageAsync()
        {
            if (_busy)
            {
                return;
            }

            if (!AppleImagePlayground.IsSupported)
            {
                SetStatus("Image Playground needs an Apple Intelligence device.", SampleTone.Warning);
                Append("Image Playground is only available on device.");
                return;
            }

            _busy = true;
            SetStatus("Generating image...", SampleTone.Working);
            _output = "Generating an image for: \"" + Trim(_prompt) + "\"";
            Append("Image generation: \"" + Trim(_prompt) + "\"");
            try
            {
                var png = await AppleImagePlayground.GenerateAsync(_prompt);
                if (_generatedImage == null)
                {
                    _generatedImage = new Texture2D(2, 2);
                }

                if (_generatedImage.LoadImage(png))
                {
                    _output = "Generated a " + _generatedImage.width + " x " +
                              _generatedImage.height + " image (shown below).";
                    SetStatus("Image generated.", SampleTone.Success);
                }
                else
                {
                    SetStatus("Could not load the generated image.", SampleTone.Error);
                }
            }
            catch (Exception exception)
            {
                Fail("Image", exception);
            }
            finally
            {
                _busy = false;
            }
        }

        private async Task RunVisionAsync(AppleVisionRequestKind kind, byte[] png)
        {
            var label = kind == AppleVisionRequestKind.RecognizeText ? "Reading text" : "Classifying";
            SetStatus(label + "...", SampleTone.Working);
            _output = string.Empty;
            Append(label + " the captured screen.");
            try
            {
                var result = await AppleVision.AnalyzeAsync(png, kind);
                _output = result.Items.Count == 0
                    ? "(no results)"
                    : string.Join("\n", result.Items);
                SetStatus(label + " complete (" + result.Items.Count + " results).", SampleTone.Success);
            }
            catch (Exception exception)
            {
                Fail("Vision", exception);
            }
            finally
            {
                _busy = false;
            }
        }

        // ----- Helpers ----------------------------------------------------------

        private void SetStatus(string status, SampleTone tone)
        {
            _status = status;
            _statusTone = tone;
        }

        private void Fail(string capability, Exception exception)
        {
            _output = exception.Message;
            SetStatus(capability + " failed.", SampleTone.Error);
            Append(capability + " error: " + exception.Message);
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
        }
    }
}
