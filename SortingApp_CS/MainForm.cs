using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SortingApp_CS
{
    public sealed class MainForm : Form
    {
        private readonly Dictionary<string, Dictionary<string, string>> localizedTexts = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, LanguageInfo> languages = new Dictionary<string, LanguageInfo>();
        private readonly Dictionary<string, List<AlgorithmInfo>> algorithmsByCategory = new Dictionary<string, List<AlgorithmInfo>>();
        private readonly List<Button> elementButtons = new List<Button>();

        private Panel topPanel;
        private DoubleBufferedPanel visualizationPanel;
        private Panel bottomPanel;
        private Label inputLabel;
        private Label categoryLabel;
        private Label algorithmLabel;
        private Label speedLabel;
        private Label outputLabel;
        private Label historyLabel;
        private TextBox inputTextBox;
        private TextBox outputTextBox;
        private TextBox historyTextBox;
        private Button submitButton;
        private Button startStopButton;
        private Button exportButton;
        private Button viewCodeButton;
        private ComboBox categoryComboBox;
        private ComboBox algorithmComboBox;
        private ComboBox languageComboBox;
        private NumericUpDown speedNumericUpDown;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        private Color movingColor;
        private Color idleColor;
        private Color completedColor;
        private string backgroundImagePath;
        private string currentLanguageCode = "vi";
        private int[] originalInputValues = new int[0];
        private int[] currentValues = new int[0];
        private int[] sortedValues = new int[0];
        private int currentDelayMilliseconds;
        private long stepCount;
        private bool isSorting;
        private bool hasCompletedResult;
        private bool isInputPlaceholderActive;
        private IntPtr cancellationFlag = IntPtr.Zero;
        private InteropHelper.UpdateCallback nativeCallback;
        private GCHandle nativeCallbackHandle;

        public MainForm()
        {
            LoadSettings();
            LoadLocalization();
            BuildAlgorithmCatalog();
            InitializeComponent();
            ApplyLocalization();
            LoadBackgroundImage();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ReleaseNativeResources();
            base.OnFormClosed(e);
        }

        private void InitializeComponent()
        {
            Text = GetText("ApplicationTitle");
            MinimumSize = new Size(980, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            DoubleBuffered = true;

            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 132,
                Padding = new Padding(14, 12, 14, 8),
                BackColor = Color.FromArgb(248, 249, 251)
            };

            visualizationPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                AutoScroll = true,
                BackgroundImageLayout = ImageLayout.Stretch
            };

            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 230,
                Padding = new Padding(14, 8, 14, 8),
                BackColor = Color.FromArgb(248, 249, 251)
            };

            inputLabel = CreateLabel();
            categoryLabel = CreateLabel();
            algorithmLabel = CreateLabel();
            speedLabel = CreateLabel();
            outputLabel = CreateLabel();
            historyLabel = CreateLabel();

            inputTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            submitButton = new Button { Width = 118, Height = 32 };
            startStopButton = new Button { Width = 110, Height = 32 };
            exportButton = new Button { Width = 110, Height = 32 };
            viewCodeButton = new Button { Width = 128, Height = 32 };

            categoryComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
            algorithmComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
            languageComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28,
                Width = 155
            };

            speedNumericUpDown = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 10000,
                Increment = 10,
                Value = 60,
                Width = 84
            };

            outputTextBox = new TextBox
            {
                ReadOnly = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.White
            };

            historyTextBox = new TextBox
            {
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                BackColor = Color.White,
                Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };

            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            statusStrip.Items.Add(statusLabel);

            submitButton.Click += SubmitButton_Click;
            startStopButton.Click += StartStopButton_Click;
            exportButton.Click += ExportButton_Click;
            viewCodeButton.Click += ViewCodeButton_Click;
            inputTextBox.Enter += InputTextBox_Enter;
            inputTextBox.Leave += InputTextBox_Leave;
            categoryComboBox.SelectedIndexChanged += CategoryComboBox_SelectedIndexChanged;
            languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            languageComboBox.DrawItem += LanguageComboBox_DrawItem;
            visualizationPanel.Resize += (sender, args) =>
            {
                if (currentValues.Length > 0)
                {
                    Color backColor = hasCompletedResult ? completedColor : idleColor;
                    Color foreColor = hasCompletedResult ? Color.Black : Color.White;
                    RenderElements(currentValues, -1, -1, backColor, foreColor);
                }
            };

            Controls.Add(visualizationPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
            Controls.Add(statusStrip);

            ArrangeTopPanel();
            ArrangeBottomPanel();
            PopulateLanguageComboBox();
            PopulateCategoryComboBox();
            SetInputPlaceholder();
            SetSortingState(false);
        }

        private void ArrangeTopPanel()
        {
            topPanel.SuspendLayout();

            inputLabel.SetBounds(14, 12, 160, 24);
            inputTextBox.SetBounds(14, 38, topPanel.Width - 484, 30);
            submitButton.SetBounds(topPanel.Width - 456, 37, 118, 32);
            languageComboBox.SetBounds(topPanel.Width - 169, 12, 155, 32);

            categoryLabel.SetBounds(14, 76, 170, 24);
            categoryComboBox.SetBounds(14, 100, 190, 30);
            algorithmLabel.SetBounds(224, 76, 170, 24);
            algorithmComboBox.SetBounds(224, 100, 190, 30);
            speedLabel.SetBounds(434, 76, 120, 24);
            speedNumericUpDown.SetBounds(434, 100, 84, 30);
            startStopButton.SetBounds(542, 99, 110, 32);
            viewCodeButton.SetBounds(666, 99, 128, 32);

            topPanel.Controls.AddRange(new Control[]
            {
                inputLabel, inputTextBox, submitButton, languageComboBox,
                categoryLabel, categoryComboBox, algorithmLabel, algorithmComboBox,
                speedLabel, speedNumericUpDown, startStopButton, viewCodeButton
            });

            topPanel.Resize += (sender, args) =>
            {
                int inputWidth = Math.Max(220, topPanel.Width - 484);
                inputTextBox.Width = inputWidth;
                submitButton.Left = inputTextBox.Right + 14;
                languageComboBox.Left = topPanel.Width - languageComboBox.Width - 14;
            };

            topPanel.ResumeLayout(false);
        }

        private void ArrangeBottomPanel()
        {
            bottomPanel.SuspendLayout();

            outputLabel.SetBounds(14, 8, 160, 24);
            outputTextBox.SetBounds(14, 34, bottomPanel.Width - 166, 30);
            exportButton.SetBounds(bottomPanel.Width - 124, 33, 110, 32);
            historyLabel.SetBounds(14, 72, 320, 24);
            historyTextBox.SetBounds(14, 98, bottomPanel.Width - 28, bottomPanel.Height - 112);

            bottomPanel.Controls.AddRange(new Control[] { outputLabel, outputTextBox, exportButton, historyLabel, historyTextBox });
            bottomPanel.Resize += (sender, args) =>
            {
                outputTextBox.Width = Math.Max(250, bottomPanel.Width - 166);
                exportButton.Left = bottomPanel.Width - exportButton.Width - 14;
                historyTextBox.Width = Math.Max(250, bottomPanel.Width - 28);
                historyTextBox.Height = Math.Max(70, bottomPanel.Height - 112);
            };

            bottomPanel.ResumeLayout(false);
        }

        private Label CreateLabel()
        {
            return new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(34, 40, 49)
            };
        }

        private void LoadSettings()
        {
            movingColor = ReadColorSetting("MovingColor", Color.FromArgb(229, 57, 53));
            idleColor = ReadColorSetting("IdleColor", Color.FromArgb(25, 118, 210));
            completedColor = ReadColorSetting("CompletedColor", Color.FromArgb(67, 160, 71));
            backgroundImagePath = ConfigurationManager.AppSettings["BackgroundImagePath"] ?? string.Empty;
        }

        private Color ReadColorSetting(string key, Color fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            try
            {
                return ColorTranslator.FromHtml(value);
            }
            catch
            {
                return Color.FromName(value);
            }
        }

        private void LoadLocalization()
        {
            string path = ResolvePath(Path.Combine("Languages", "localization.xml"));
            XDocument document = XDocument.Load(path);

            foreach (XElement languageElement in document.Root.Elements("language"))
            {
                string code = (string)languageElement.Attribute("code");
                string name = (string)languageElement.Attribute("name");
                string flag = (string)languageElement.Attribute("flag");

                languages[code] = new LanguageInfo(code, name, flag);
                localizedTexts[code] = languageElement.Elements("text")
                    .ToDictionary(item => (string)item.Attribute("key"), item => item.Value);
            }
        }

        private void BuildAlgorithmCatalog()
        {
            algorithmsByCategory["BasicCategory"] = new List<AlgorithmInfo>
            {
                new AlgorithmInfo("BubbleSort", SortingAlgorithmCode.BubbleSort),
                new AlgorithmInfo("InsertionSort", SortingAlgorithmCode.InsertionSort),
                new AlgorithmInfo("SelectionSort", SortingAlgorithmCode.SelectionSort),
                new AlgorithmInfo("CocktailSort", SortingAlgorithmCode.CocktailSort),
                new AlgorithmInfo("CombSort", SortingAlgorithmCode.CombSort),
                new AlgorithmInfo("GnomeSort", SortingAlgorithmCode.GnomeSort),
                new AlgorithmInfo("OddEvenSort", SortingAlgorithmCode.OddEvenSort),
                new AlgorithmInfo("ShellSort", SortingAlgorithmCode.ShellSort),
                new AlgorithmInfo("DoubleSelectionSort", SortingAlgorithmCode.DoubleSelectionSort),
                new AlgorithmInfo("CycleSort", SortingAlgorithmCode.CycleSort),
                new AlgorithmInfo("PancakeSort", SortingAlgorithmCode.PancakeSort),
                new AlgorithmInfo("ExchangeSort", SortingAlgorithmCode.ExchangeSort),
                new AlgorithmInfo("BinaryInsertionSort", SortingAlgorithmCode.BinaryInsertionSort)
            };

            algorithmsByCategory["DivideCategory"] = new List<AlgorithmInfo>
            {
                new AlgorithmInfo("MergeSort", SortingAlgorithmCode.MergeSort),
                new AlgorithmInfo("QuickSort", SortingAlgorithmCode.QuickSort),
                new AlgorithmInfo("HeapSort", SortingAlgorithmCode.HeapSort),
                new AlgorithmInfo("BitonicSort", SortingAlgorithmCode.BitonicSort),
                new AlgorithmInfo("StoogeSort", SortingAlgorithmCode.StoogeSort),
                new AlgorithmInfo("TournamentSort", SortingAlgorithmCode.TournamentSort)
            };

            algorithmsByCategory["DistributionCategory"] = new List<AlgorithmInfo>
            {
                new AlgorithmInfo("RadixSort", SortingAlgorithmCode.RadixSort),
                new AlgorithmInfo("CountingSort", SortingAlgorithmCode.CountingSort),
                new AlgorithmInfo("BucketSort", SortingAlgorithmCode.BucketSort),
                new AlgorithmInfo("PigeonholeSort", SortingAlgorithmCode.PigeonholeSort),
                new AlgorithmInfo("BeadSort", SortingAlgorithmCode.BeadSort),
                new AlgorithmInfo("FlashSort", SortingAlgorithmCode.FlashSort)
            };

            algorithmsByCategory["HybridCategory"] = new List<AlgorithmInfo>
            {
                new AlgorithmInfo("IntroSort", SortingAlgorithmCode.IntroSort),
                new AlgorithmInfo("TimSort", SortingAlgorithmCode.TimSort)
            };

            algorithmsByCategory["AdvancedCategory"] = new List<AlgorithmInfo>
            {
                new AlgorithmInfo("TreeSort", SortingAlgorithmCode.TreeSort),
                new AlgorithmInfo("PatienceSort", SortingAlgorithmCode.PatienceSort),
                new AlgorithmInfo("StrandSort", SortingAlgorithmCode.StrandSort)
            };
        }

        private void PopulateLanguageComboBox()
        {
            languageComboBox.Items.Clear();
            foreach (LanguageInfo language in languages.Values)
            {
                languageComboBox.Items.Add(language);
            }

            languageComboBox.SelectedItem = languageComboBox.Items.Cast<LanguageInfo>()
                .FirstOrDefault(item => item.Code == currentLanguageCode);
        }

        private void PopulateCategoryComboBox()
        {
            string selectedKey = (categoryComboBox.SelectedItem as CategoryInfo)?.Key;
            categoryComboBox.Items.Clear();

            foreach (string categoryKey in algorithmsByCategory.Keys)
            {
                categoryComboBox.Items.Add(new CategoryInfo(categoryKey, GetText(categoryKey)));
            }

            CategoryInfo selectedItem = categoryComboBox.Items.Cast<CategoryInfo>()
                .FirstOrDefault(item => item.Key == selectedKey)
                ?? categoryComboBox.Items.Cast<CategoryInfo>().FirstOrDefault();

            categoryComboBox.SelectedItem = selectedItem;
        }

        private void PopulateAlgorithmComboBox(string categoryKey)
        {
            string selectedKey = (algorithmComboBox.SelectedItem as AlgorithmInfo)?.Key;
            algorithmComboBox.Items.Clear();

            if (!algorithmsByCategory.ContainsKey(categoryKey))
            {
                return;
            }

            foreach (AlgorithmInfo algorithm in algorithmsByCategory[categoryKey])
            {
                algorithm.DisplayName = GetText(algorithm.Key);
                algorithmComboBox.Items.Add(algorithm);
            }

            AlgorithmInfo selectedItem = algorithmComboBox.Items.Cast<AlgorithmInfo>()
                .FirstOrDefault(item => item.Key == selectedKey)
                ?? algorithmComboBox.Items.Cast<AlgorithmInfo>().FirstOrDefault();

            algorithmComboBox.SelectedItem = selectedItem;
        }

        private void ApplyLocalization()
        {
            Text = GetText("ApplicationTitle");
            inputLabel.Text = GetText("InputLabel");
            categoryLabel.Text = GetText("CategoryLabel");
            algorithmLabel.Text = GetText("AlgorithmLabel");
            speedLabel.Text = GetText("SpeedLabel");
            outputLabel.Text = GetText("OutputLabel");
            UpdateHistoryLabel();
            if (isInputPlaceholderActive)
            {
                SetInputPlaceholder();
            }
            submitButton.Text = GetText("SubmitButton");
            exportButton.Text = GetText("ExportButton");
            viewCodeButton.Text = GetText("ViewCodeButton");
            startStopButton.Text = isSorting ? GetText("StopButton") : GetText("StartButton");

            PopulateCategoryComboBox();
            CategoryInfo category = categoryComboBox.SelectedItem as CategoryInfo;
            if (category != null)
            {
                PopulateAlgorithmComboBox(category.Key);
            }
        }

        private string GetText(string key)
        {
            Dictionary<string, string> textSet;
            if (localizedTexts.TryGetValue(currentLanguageCode, out textSet) && textSet.ContainsKey(key))
            {
                return textSet[key];
            }

            if (localizedTexts.TryGetValue("en", out textSet) && textSet.ContainsKey(key))
            {
                return textSet[key];
            }

            return key;
        }

        private void LoadBackgroundImage()
        {
            string path = ResolvePath(backgroundImagePath);
            if (File.Exists(path))
            {
                visualizationPanel.BackgroundImage = Image.FromFile(path);
            }
        }

        private string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string candidate = Path.Combine(baseDirectory, path);
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", path));
        }

        private void SubmitButton_Click(object sender, EventArgs e)
        {
            if (isSorting)
            {
                return;
            }

            int[] parsedValues;
            if (isInputPlaceholderActive || !TryParseInput(inputTextBox.Text, out parsedValues))
            {
                MessageBox.Show(GetText("InvalidInput"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            currentValues = parsedValues;
            originalInputValues = parsedValues.ToArray();
            isInputPlaceholderActive = false;
            inputTextBox.ForeColor = SystemColors.WindowText;
            sortedValues = new int[0];
            hasCompletedResult = false;
            outputTextBox.Clear();
            ClearSortHistory();
            RenderElements(currentValues, -1, -1, idleColor, Color.White);
            statusLabel.Text = string.Empty;
        }

        private void InputTextBox_Enter(object sender, EventArgs e)
        {
            if (!isInputPlaceholderActive)
            {
                return;
            }

            isInputPlaceholderActive = false;
            inputTextBox.Clear();
            inputTextBox.ForeColor = SystemColors.WindowText;
        }

        private void InputTextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(inputTextBox.Text) && currentValues.Length == 0)
            {
                SetInputPlaceholder();
            }
        }

        private void SetInputPlaceholder()
        {
            isInputPlaceholderActive = true;
            inputTextBox.Text = GetText("InputPlaceholder");
            inputTextBox.ForeColor = SystemColors.GrayText;
        }

        private bool TryParseInput(string text, out int[] values)
        {
            values = new int[0];
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] parts = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> parsedValues = new List<int>();

            foreach (string part in parts)
            {
                int value;
                if (!int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    return false;
                }

                parsedValues.Add(value);
            }

            values = parsedValues.ToArray();
            return values.Length > 0;
        }

        private async void StartStopButton_Click(object sender, EventArgs e)
        {
            if (isSorting)
            {
                RequestCancellation();
                return;
            }

            if (currentValues.Length == 0)
            {
                MessageBox.Show(GetText("NoData"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await StartSortingAsync();
        }

        private async Task StartSortingAsync()
        {
            SetSortingState(true);
            hasCompletedResult = false;
            ClearSortHistory();
            int[] workingValues = currentValues.ToArray();
            EnsureNativeResources();

            try
            {
                AlgorithmInfo algorithm = algorithmComboBox.SelectedItem as AlgorithmInfo;
                int algorithmCode = algorithm == null ? 0 : (int)algorithm.Code;
                int delayMilliseconds = (int)speedNumericUpDown.Value;
                currentDelayMilliseconds = delayMilliseconds;

                await Task.Run(() =>
                {
                    InteropHelper.StartSort(
                        workingValues,
                        workingValues.Length,
                        algorithmCode,
                        nativeCallback,
                        cancellationFlag,
                        delayMilliseconds);
                });

                bool wasCancelled = Marshal.ReadByte(cancellationFlag) != 0;
                currentValues = workingValues;
                sortedValues = workingValues.ToArray();
                outputTextBox.Text = string.Join(", ", sortedValues);

                if (wasCancelled)
                {
                    RenderElements(currentValues, -1, -1, idleColor, Color.White);
                    AppendSortHistoryFooter(GetText("SortingStopped"));
                    statusLabel.Text = GetText("SortingStopped");
                }
                else
                {
                    hasCompletedResult = true;
                    await BlinkCompletionAsync();
                    AppendSortHistoryFooter(GetText("SortingCompleted"));
                    statusLabel.Text = GetText("SortingCompleted");
                }
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show("SortingLogic_CPP.dll was not found next to the application executable.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (BadImageFormatException)
            {
                MessageBox.Show("The C++ DLL platform does not match the C# application platform.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                currentDelayMilliseconds = 0;
                SetSortingState(false);
                Marshal.WriteByte(cancellationFlag, 0);
            }
        }

        private void EnsureNativeResources()
        {
            if (cancellationFlag == IntPtr.Zero)
            {
                cancellationFlag = Marshal.AllocHGlobal(1);
            }

            Marshal.WriteByte(cancellationFlag, 0);

            if (nativeCallback == null)
            {
                nativeCallback = NativeUpdateCallback;
                nativeCallbackHandle = GCHandle.Alloc(nativeCallback);
            }
        }

        private void ReleaseNativeResources()
        {
            if (nativeCallbackHandle.IsAllocated)
            {
                nativeCallbackHandle.Free();
            }

            if (cancellationFlag != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(cancellationFlag);
                cancellationFlag = IntPtr.Zero;
            }
        }

        private void RequestCancellation()
        {
            if (cancellationFlag != IntPtr.Zero)
            {
                Marshal.WriteByte(cancellationFlag, 1);
            }

            startStopButton.Enabled = false;
        }

        private void NativeUpdateCallback(IntPtr arrayPointer, int size, int activeIdx1, int activeIdx2)
        {
            if (IsDisposed || arrayPointer == IntPtr.Zero || size <= 0)
            {
                return;
            }

            int[] snapshot = new int[size];
            Marshal.Copy(arrayPointer, snapshot, 0, size);

            Action updateAction = () =>
            {
                int[] previousValues = currentValues;
                bool shouldAnimateSwap = ShouldAnimateSwap(previousValues, snapshot, activeIdx1, activeIdx2);
                currentValues = snapshot;
                AppendSortHistoryStep(snapshot, activeIdx1, activeIdx2);

                if (shouldAnimateSwap)
                {
                    AnimateSwapTransition(previousValues, snapshot, activeIdx1, activeIdx2);
                }

                RenderElements(snapshot, activeIdx1, activeIdx2, idleColor, Color.White);
            };

            if (visualizationPanel.InvokeRequired)
            {
                visualizationPanel.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        private void RenderElements(int[] values, int activeIdx1, int activeIdx2, Color defaultBackColor, Color defaultForeColor)
        {
            visualizationPanel.SuspendLayout();

            EnsureElementButtonCount(values.Length);
            int availableWidth = Math.Max(1, visualizationPanel.ClientSize.Width - visualizationPanel.Padding.Horizontal);
            int buttonWidth = Math.Max(56, Math.Min(100, (availableWidth - 12 * Math.Max(0, values.Length - 1)) / Math.Max(1, values.Length)));
            int buttonHeight = 42;
            int spacing = 12;

            for (int i = 0; i < elementButtons.Count; i++)
            {
                Button button = elementButtons[i];
                bool isActive = i == activeIdx1 || i == activeIdx2;
                int rowCapacity = Math.Max(1, availableWidth / (buttonWidth + spacing));
                int row = i / rowCapacity;
                int column = i % rowCapacity;

                button.Text = values[i].ToString(CultureInfo.InvariantCulture);
                button.Width = buttonWidth;
                button.Height = buttonHeight;
                button.Left = visualizationPanel.Padding.Left + column * (buttonWidth + spacing);
                button.Top = visualizationPanel.Padding.Top + row * (buttonHeight + spacing);
                button.BackColor = isActive ? movingColor : defaultBackColor;
                button.ForeColor = defaultForeColor;
                button.Visible = true;
            }

            visualizationPanel.ResumeLayout();
        }

        private bool ShouldAnimateSwap(int[] previousValues, int[] nextValues, int activeIdx1, int activeIdx2)
        {
            if (previousValues == null || nextValues == null)
            {
                return false;
            }

            if (activeIdx1 < 0 || activeIdx2 < 0 || activeIdx1 == activeIdx2)
            {
                return false;
            }

            if (activeIdx1 >= previousValues.Length || activeIdx2 >= previousValues.Length ||
                activeIdx1 >= nextValues.Length || activeIdx2 >= nextValues.Length)
            {
                return false;
            }

            return previousValues[activeIdx1] == nextValues[activeIdx2] &&
                   previousValues[activeIdx2] == nextValues[activeIdx1] &&
                   previousValues[activeIdx1] != previousValues[activeIdx2];
        }

        private void AnimateSwapTransition(int[] previousValues, int[] nextValues, int activeIdx1, int activeIdx2)
        {
            EnsureElementButtonCount(nextValues.Length);

            Button firstSourceButton = elementButtons[activeIdx1];
            Button secondSourceButton = elementButtons[activeIdx2];
            Rectangle firstStartBounds = firstSourceButton.Bounds;
            Rectangle secondStartBounds = secondSourceButton.Bounds;

            Button firstAnimationButton = CreateAnimationButton(previousValues[activeIdx1], firstStartBounds);
            Button secondAnimationButton = CreateAnimationButton(previousValues[activeIdx2], secondStartBounds);

            firstSourceButton.Visible = false;
            secondSourceButton.Visible = false;
            visualizationPanel.Controls.Add(firstAnimationButton);
            visualizationPanel.Controls.Add(secondAnimationButton);
            firstAnimationButton.BringToFront();
            secondAnimationButton.BringToFront();

            int durationMilliseconds = Math.Min(900, Math.Max(450, currentDelayMilliseconds / 3));
            int frameCount = Math.Max(18, durationMilliseconds / 16);

            for (int frame = 0; frame <= frameCount; frame++)
            {
                float progress = frame / (float)frameCount;
                float easedProgress = EaseInOut(progress);

                firstAnimationButton.Left = Interpolate(firstStartBounds.Left, secondStartBounds.Left, easedProgress);
                firstAnimationButton.Top = Interpolate(firstStartBounds.Top, secondStartBounds.Top, easedProgress);
                secondAnimationButton.Left = Interpolate(secondStartBounds.Left, firstStartBounds.Left, easedProgress);
                secondAnimationButton.Top = Interpolate(secondStartBounds.Top, firstStartBounds.Top, easedProgress);

                visualizationPanel.Refresh();
                Application.DoEvents();
                Thread.Sleep(Math.Max(1, durationMilliseconds / frameCount));
            }

            visualizationPanel.Controls.Remove(firstAnimationButton);
            visualizationPanel.Controls.Remove(secondAnimationButton);
            firstAnimationButton.Dispose();
            secondAnimationButton.Dispose();
            firstSourceButton.Visible = true;
            secondSourceButton.Visible = true;
        }

        private Button CreateAnimationButton(int value, Rectangle bounds)
        {
            Button button = new Button
            {
                Text = value.ToString(CultureInfo.InvariantCulture),
                Bounds = bounds,
                BackColor = movingColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(Font, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderColor = Color.White;
            return button;
        }

        private int Interpolate(int start, int end, float progress)
        {
            return start + (int)Math.Round((end - start) * progress);
        }

        private float EaseInOut(float value)
        {
            return value < 0.5F
                ? 2F * value * value
                : 1F - (float)Math.Pow(-2F * value + 2F, 2) / 2F;
        }

        private void EnsureElementButtonCount(int count)
        {
            while (elementButtons.Count < count)
            {
                Button button = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(Font, FontStyle.Bold),
                    UseVisualStyleBackColor = false
                };

                button.FlatAppearance.BorderColor = Color.White;
                button.Click += ElementButton_Click;
                elementButtons.Add(button);
                visualizationPanel.Controls.Add(button);
            }

            for (int i = 0; i < elementButtons.Count; i++)
            {
                elementButtons[i].Visible = i < count;
            }
        }

        private void ElementButton_Click(object sender, EventArgs e)
        {
            if (isSorting)
            {
                return;
            }

            Button button = sender as Button;
            int index = elementButtons.IndexOf(button);
            if (index < 0 || index >= currentValues.Length)
            {
                return;
            }

            using (ValuePromptForm prompt = new ValuePromptForm(GetText("EditValueTitle"), GetText("EditValuePrompt"), currentValues[index], GetText("OkButton"), GetText("CancelButton")))
            {
                if (prompt.ShowDialog(this) == DialogResult.OK)
                {
                    currentValues[index] = prompt.Value;
                    originalInputValues = currentValues.ToArray();
                    isInputPlaceholderActive = false;
                    inputTextBox.ForeColor = SystemColors.WindowText;
                    inputTextBox.Text = string.Join(", ", currentValues);
                    hasCompletedResult = false;
                    outputTextBox.Clear();
                    ClearSortHistory();
                    RenderElements(currentValues, -1, -1, idleColor, Color.White);
                }
            }
        }

        private void ClearSortHistory()
        {
            stepCount = 0;
            if (historyTextBox != null)
            {
                historyTextBox.Clear();
            }

            UpdateHistoryLabel();
        }

        private void AppendSortHistoryStep(int[] values, int activeIdx1, int activeIdx2)
        {
            stepCount++;
            UpdateHistoryLabel();

            string activeText = activeIdx1 >= 0 || activeIdx2 >= 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}, {1}", activeIdx1, activeIdx2)
                : "-";

            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1:0000} | {2}: {3} | {4}: {5}{6}",
                GetText("StepLabel"),
                stepCount,
                GetText("ActiveIndexLabel"),
                activeText,
                GetText("ArrayLabel"),
                string.Join(", ", values),
                Environment.NewLine);

            historyTextBox.AppendText(line);
        }

        private void AppendSortHistoryFooter(string message)
        {
            historyTextBox.AppendText(string.Format(CultureInfo.InvariantCulture, "{0}{1}", message, Environment.NewLine));
        }

        private void UpdateHistoryLabel()
        {
            if (historyLabel != null)
            {
                historyLabel.Text = string.Format(CultureInfo.CurrentCulture, GetText("HistoryLabelFormat"), stepCount);
            }
        }

        private void ViewCodeButton_Click(object sender, EventArgs e)
        {
            AlgorithmInfo algorithm = algorithmComboBox.SelectedItem as AlgorithmInfo;
            if (algorithm == null)
            {
                return;
            }

            using (PseudoCodeForm form = new PseudoCodeForm(
                string.Format(CultureInfo.CurrentCulture, GetText("PseudoCodeTitleFormat"), algorithm.DisplayName),
                GetPseudoCode(algorithm.Code),
                GetText("CloseButton")))
            {
                form.ShowDialog(this);
            }
        }

        private string GetPseudoCode(SortingAlgorithmCode algorithmCode)
        {
            switch (algorithmCode)
            {
                case SortingAlgorithmCode.BubbleSort:
                    return "BubbleSort(array)\r\n"
                        + "  for i = 0 to n - 2\r\n"
                        + "    for j = 0 to n - i - 2\r\n"
                        + "      if array[j] > array[j + 1]\r\n"
                        + "        swap array[j], array[j + 1]";
                case SortingAlgorithmCode.InsertionSort:
                    return "InsertionSort(array)\r\n"
                        + "  for i = 1 to n - 1\r\n"
                        + "    key = array[i]\r\n"
                        + "    j = i - 1\r\n"
                        + "    while j >= 0 and array[j] > key\r\n"
                        + "      array[j + 1] = array[j]\r\n"
                        + "      j = j - 1\r\n"
                        + "    array[j + 1] = key";
                case SortingAlgorithmCode.MergeSort:
                    return "MergeSort(array, left, right)\r\n"
                        + "  if left >= right return\r\n"
                        + "  middle = (left + right) / 2\r\n"
                        + "  MergeSort(array, left, middle)\r\n"
                        + "  MergeSort(array, middle + 1, right)\r\n"
                        + "  Merge two sorted halves";
                case SortingAlgorithmCode.QuickSort:
                    return "QuickSort(array, low, high)\r\n"
                        + "  if low >= high return\r\n"
                        + "  pivotIndex = Partition(array, low, high)\r\n"
                        + "  QuickSort(array, low, pivotIndex - 1)\r\n"
                        + "  QuickSort(array, pivotIndex + 1, high)\r\n\r\n"
                        + "Partition(array, low, high)\r\n"
                        + "  pivot = array[high]\r\n"
                        + "  move smaller values before pivot\r\n"
                        + "  place pivot in final position";
                case SortingAlgorithmCode.HeapSort:
                    return "HeapSort(array)\r\n"
                        + "  Build max heap\r\n"
                        + "  for end = n - 1 down to 1\r\n"
                        + "    swap array[0], array[end]\r\n"
                        + "    Heapify(array, 0, end)";
                case SortingAlgorithmCode.RadixSort:
                    return "RadixSort(array)\r\n"
                        + "  shift values if negative numbers exist\r\n"
                        + "  for exponent = 1; max / exponent > 0; exponent *= 10\r\n"
                        + "    CountingSortByDigit(array, exponent)\r\n"
                        + "  restore shifted values";
                case SortingAlgorithmCode.CountingSort:
                    return "CountingSort(array)\r\n"
                        + "  find minimum and maximum\r\n"
                        + "  create count array\r\n"
                        + "  count each value\r\n"
                        + "  write values back in sorted order";
                case SortingAlgorithmCode.IntroSort:
                    return "IntroSort(array)\r\n"
                        + "  depthLimit = 2 * log2(n)\r\n"
                        + "  QuickSort while depthLimit is safe\r\n"
                        + "  use HeapSort when depthLimit reaches zero\r\n"
                        + "  use InsertionSort for small partitions";
                case SortingAlgorithmCode.SelectionSort:
                    return "SelectionSort(array)\r\n"
                        + "  for i = 0 to n - 2\r\n"
                        + "    minIndex = i\r\n"
                        + "    for j = i + 1 to n - 1\r\n"
                        + "      if array[j] < array[minIndex]\r\n"
                        + "        minIndex = j\r\n"
                        + "    swap array[i], array[minIndex]";
                case SortingAlgorithmCode.CocktailSort:
                    return "CocktailSort(array)\r\n"
                        + "  repeat while swapped\r\n"
                        + "    scan left to right and swap adjacent inversions\r\n"
                        + "    scan right to left and swap adjacent inversions";
                case SortingAlgorithmCode.CombSort:
                    return "CombSort(array)\r\n"
                        + "  gap = n\r\n"
                        + "  while gap > 1 or swapped\r\n"
                        + "    gap = gap / shrinkFactor\r\n"
                        + "    compare and swap elements separated by gap";
                case SortingAlgorithmCode.GnomeSort:
                    return "GnomeSort(array)\r\n"
                        + "  index = 0\r\n"
                        + "  while index < n\r\n"
                        + "    if current pair is ordered move right\r\n"
                        + "    otherwise swap and move left";
                case SortingAlgorithmCode.OddEvenSort:
                    return "OddEvenSort(array)\r\n"
                        + "  repeat until sorted\r\n"
                        + "    compare odd-even adjacent pairs\r\n"
                        + "    compare even-odd adjacent pairs";
                case SortingAlgorithmCode.ShellSort:
                    return "ShellSort(array)\r\n"
                        + "  gap = n / 2\r\n"
                        + "  while gap > 0\r\n"
                        + "    insertion sort elements separated by gap\r\n"
                        + "    gap = gap / 2";
                case SortingAlgorithmCode.CycleSort:
                    return "CycleSort(array)\r\n"
                        + "  for each cycle start\r\n"
                        + "    count smaller values to find final position\r\n"
                        + "    rotate values until cycle returns to start";
                case SortingAlgorithmCode.PancakeSort:
                    return "PancakeSort(array)\r\n"
                        + "  for currentSize = n down to 2\r\n"
                        + "    find maximum in prefix\r\n"
                        + "    flip maximum to front\r\n"
                        + "    flip it into final position";
                case SortingAlgorithmCode.ExchangeSort:
                    return "ExchangeSort(array)\r\n"
                        + "  for i = 0 to n - 2\r\n"
                        + "    for j = i + 1 to n - 1\r\n"
                        + "      if array[i] > array[j]\r\n"
                        + "        swap array[i], array[j]";
                case SortingAlgorithmCode.BinaryInsertionSort:
                    return "BinaryInsertionSort(array)\r\n"
                        + "  for each item from left to right\r\n"
                        + "    binary search insertion position\r\n"
                        + "    shift larger values right\r\n"
                        + "    place item";
                case SortingAlgorithmCode.BitonicSort:
                    return "BitonicSort(array)\r\n"
                        + "  recursively build ascending and descending halves\r\n"
                        + "  compare distant pairs\r\n"
                        + "  recursively merge into ascending order";
                case SortingAlgorithmCode.StoogeSort:
                    return "StoogeSort(array, left, right)\r\n"
                        + "  swap endpoints if needed\r\n"
                        + "  sort first two thirds\r\n"
                        + "  sort last two thirds\r\n"
                        + "  sort first two thirds again";
                case SortingAlgorithmCode.TournamentSort:
                    return "TournamentSort(array)\r\n"
                        + "  repeatedly find the smallest remaining value\r\n"
                        + "  append it to output\r\n"
                        + "  write output back to array";
                case SortingAlgorithmCode.BucketSort:
                    return "BucketSort(array)\r\n"
                        + "  distribute values into buckets by range\r\n"
                        + "  sort each bucket\r\n"
                        + "  concatenate buckets";
                case SortingAlgorithmCode.PigeonholeSort:
                    return "PigeonholeSort(array)\r\n"
                        + "  create holes for value range\r\n"
                        + "  count values in each hole\r\n"
                        + "  write values back in order";
                case SortingAlgorithmCode.BeadSort:
                    return "BeadSort(array)\r\n"
                        + "  represent non-negative values as beads\r\n"
                        + "  let beads fall by column counts\r\n"
                        + "  read rows back as sorted values";
                case SortingAlgorithmCode.FlashSort:
                    return "FlashSort(array)\r\n"
                        + "  classify values into value ranges\r\n"
                        + "  permute values into classes\r\n"
                        + "  finish with insertion sort";
                case SortingAlgorithmCode.TimSort:
                    return "TimSort(array)\r\n"
                        + "  sort small runs with insertion sort\r\n"
                        + "  merge runs with increasing sizes\r\n"
                        + "  finish when one sorted run remains";
                case SortingAlgorithmCode.DoubleSelectionSort:
                    return "DoubleSelectionSort(array)\r\n"
                        + "  left = 0, right = n - 1\r\n"
                        + "  while left < right\r\n"
                        + "    find minimum and maximum in the active range\r\n"
                        + "    move minimum to left and maximum to right\r\n"
                        + "    shrink the active range";
                case SortingAlgorithmCode.TreeSort:
                    return "TreeSort(array)\r\n"
                        + "  insert every value into a binary search tree\r\n"
                        + "  traverse the tree in order\r\n"
                        + "  write sorted values back";
                case SortingAlgorithmCode.PatienceSort:
                    return "PatienceSort(array)\r\n"
                        + "  place values onto ordered piles\r\n"
                        + "  repeatedly take the smallest pile top\r\n"
                        + "  write extracted values back";
                case SortingAlgorithmCode.StrandSort:
                    return "StrandSort(array)\r\n"
                        + "  pull increasing strands from input\r\n"
                        + "  merge each strand into output\r\n"
                        + "  repeat until input is empty";
                default:
                    return string.Empty;
            }
        }

        private async Task BlinkCompletionAsync()
        {
            for (int i = 0; i < 3; i++)
            {
                RenderElements(sortedValues, -1, -1, Color.White, Color.Black);
                await Task.Delay(160);
                RenderElements(sortedValues, -1, -1, completedColor, Color.Black);
                await Task.Delay(160);
            }

            RenderElements(sortedValues, -1, -1, completedColor, Color.Black);
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (!hasCompletedResult || sortedValues.Length == 0)
            {
                MessageBox.Show(GetText("ExportNoResult"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = GetText("TextDialogTitle");
                dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                dialog.FileName = "sorting-report.txt";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                AlgorithmInfo algorithm = algorithmComboBox.SelectedItem as AlgorithmInfo;
                string algorithmName = algorithm == null ? string.Empty : algorithm.DisplayName;
                string report = BuildTextReport(algorithmName);

                File.WriteAllText(dialog.FileName, report, Encoding.UTF8);
                MessageBox.Show(GetText("ExportSuccess"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string BuildTextReport(string algorithmName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(GetText("ReportTitle"));
            builder.AppendLine(new string('=', 48));
            builder.AppendLine(GetText("ReportTimestampLabel") + ": " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            builder.AppendLine(GetText("ReportAlgorithmLabel") + ": " + algorithmName);
            builder.AppendLine();
            builder.AppendLine(GetText("ReportInputLabel"));
            builder.AppendLine(string.Join(", ", originalInputValues));
            builder.AppendLine();
            builder.AppendLine(GetText("ReportProcessLabel"));
            builder.AppendLine(string.IsNullOrWhiteSpace(historyTextBox.Text) ? GetText("ReportNoProcessLabel") : historyTextBox.Text.TrimEnd());
            builder.AppendLine();
            builder.AppendLine(GetText("ReportResultLabel"));
            builder.AppendLine(string.Join(", ", sortedValues));

            return builder.ToString();
        }

        private void CategoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            CategoryInfo category = categoryComboBox.SelectedItem as CategoryInfo;
            if (category != null)
            {
                PopulateAlgorithmComboBox(category.Key);
            }
        }

        private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LanguageInfo language = languageComboBox.SelectedItem as LanguageInfo;
            if (language == null)
            {
                return;
            }

            currentLanguageCode = language.Code;
            ApplyLocalization();
            languageComboBox.Invalidate();
        }

        private void LanguageComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0)
            {
                return;
            }

            LanguageInfo language = (LanguageInfo)languageComboBox.Items[e.Index];
            Rectangle flagRectangle = new Rectangle(e.Bounds.Left + 6, e.Bounds.Top + 6, 24, 16);
            string flagPath = ResolvePath(Path.Combine("Resources", "Icons", language.FlagFileName));

            if (File.Exists(flagPath))
            {
                using (Image flagImage = Image.FromFile(flagPath))
                {
                    e.Graphics.DrawImage(flagImage, flagRectangle);
                }
            }

            using (Brush textBrush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(language.Name, e.Font, textBrush, e.Bounds.Left + 38, e.Bounds.Top + 6);
            }

            e.DrawFocusRectangle();
        }

        private void SetSortingState(bool sorting)
        {
            isSorting = sorting;
            startStopButton.Enabled = true;
            startStopButton.Text = sorting ? GetText("StopButton") : GetText("StartButton");
            startStopButton.BackColor = sorting ? Color.Firebrick : SystemColors.Control;
            startStopButton.ForeColor = sorting ? Color.White : SystemColors.ControlText;
            submitButton.Enabled = !sorting;
            categoryComboBox.Enabled = !sorting;
            algorithmComboBox.Enabled = !sorting;
            languageComboBox.Enabled = !sorting;
            speedNumericUpDown.Enabled = !sorting;
            viewCodeButton.Enabled = !sorting;
        }

        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }

        private sealed class LanguageInfo
        {
            public LanguageInfo(string code, string name, string flagFileName)
            {
                Code = code;
                Name = name;
                FlagFileName = flagFileName;
            }

            public string Code { get; private set; }
            public string Name { get; private set; }
            public string FlagFileName { get; private set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private sealed class CategoryInfo
        {
            public CategoryInfo(string key, string displayName)
            {
                Key = key;
                DisplayName = displayName;
            }

            public string Key { get; private set; }
            public string DisplayName { get; private set; }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        private sealed class AlgorithmInfo
        {
            public AlgorithmInfo(string key, SortingAlgorithmCode code)
            {
                Key = key;
                Code = code;
                DisplayName = key;
            }

            public string Key { get; private set; }
            public SortingAlgorithmCode Code { get; private set; }
            public string DisplayName { get; set; }

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }

    internal sealed class ValuePromptForm : Form
    {
        private readonly NumericUpDown valueNumericUpDown;

        public ValuePromptForm(string title, string prompt, int initialValue, string okText, string cancelText)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(320, 128);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            Label promptLabel = new Label
            {
                Text = prompt,
                Left = 12,
                Top = 12,
                Width = 296,
                Height = 24
            };

            valueNumericUpDown = new NumericUpDown
            {
                Left = 12,
                Top = 42,
                Width = 296,
                Minimum = int.MinValue,
                Maximum = int.MaxValue,
                Value = initialValue
            };

            Button okButton = new Button
            {
                Text = okText,
                Left = 112,
                Top = 84,
                Width = 92,
                DialogResult = DialogResult.OK
            };

            Button cancelButton = new Button
            {
                Text = cancelText,
                Left = 216,
                Top = 84,
                Width = 92,
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[] { promptLabel, valueNumericUpDown, okButton, cancelButton });
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        public int Value
        {
            get { return (int)valueNumericUpDown.Value; }
        }
    }

    internal sealed class PseudoCodeForm : Form
    {
        public PseudoCodeForm(string title, string pseudoCode, string closeText)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(620, 430);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            TextBox pseudoCodeTextBox = new TextBox
            {
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
                Text = pseudoCode,
                Left = 12,
                Top = 12,
                Width = ClientSize.Width - 24,
                Height = ClientSize.Height - 64,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
            };

            Button closeButton = new Button
            {
                Text = closeText,
                Width = 96,
                Height = 32,
                Left = ClientSize.Width - 108,
                Top = ClientSize.Height - 44,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                DialogResult = DialogResult.OK
            };

            Controls.AddRange(new Control[] { pseudoCodeTextBox, closeButton });
            AcceptButton = closeButton;
            CancelButton = closeButton;
        }
    }
}
