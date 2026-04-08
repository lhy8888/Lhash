using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.AppLifecycle;
using SunJWBase;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FilesHashWUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private enum MainPageControlStat
        {
            MainPageNone = 0, // clear stat
            MainPageCalcIng,  // calculating
            MainPageCalcFinish, // calculating finished/stopped
            MainPageVerify, // verifying
            MainPageWaitingExit, // waiting thread stop and exit
        };

        private const string KeyUppercase = "Uppercase";
        private const string KeyHashAlgorithmPrefix = "HashAlgorithm.";
        private const string ArgPaths = "-paths";

        private MainWindow m_mainWindow = null;
        private ResourceLoader m_resourceLoaderMain = WinUIHelper.GetCurrentResourceLoader();

        private bool m_pageInited = false;
        private bool m_pageLoaded = false;
        private bool m_pendingScrollToBottom = false;

        private ContentDialog m_dialogFind = null;
        private TextBox m_textBoxFindHash = null;

        private Paragraph m_paragraphMain = null;
        private Paragraph m_paragraphResult = null;
        private Paragraph m_paragraphFind = null;
        private List<Hyperlink> m_hyperlinksMain = null;
        private List<Hyperlink> m_hyperlinksResult = [];
        private List<Hyperlink> m_hyperlinksFind = [];
        private MenuFlyout m_menuFlyoutTextMain = null;
        private Hyperlink m_hyperlinkClicked = null;
        private Run m_runPrepare = null;

        private MainPageControlStat m_mainPageStat;

        private bool m_uppercaseChecked = false;
        private HashAlgorithmDescriptorNet[] m_hashAlgorithms = Array.Empty<HashAlgorithmDescriptorNet>();
        private Dictionary<string, CheckBox> m_hashAlgorithmCheckBoxes = [];

        private int m_inMainQueue = 0;
        private int m_outMainQueue = 0;
        private const int m_maxDiffQueue = 3;
        List<Inline> m_inlinesQueue = [];

        private long m_calcStartTime = 0;
        private long m_calcEndTime = 0;
        private int m_totalProgressValue = 0;
        private ulong m_totalSizeSnapshot = 0;
        private string m_selectedAlgorithmsSummary = string.Empty;
        private DispatcherQueueTimer m_runtimeStatusTimer = null;
        private ObservableCollection<FileTaskProgressItem> m_fileTaskItems = [];
        private Dictionary<string, FileTaskProgressItem> m_fileTaskIndex = new(StringComparer.OrdinalIgnoreCase);

        private static readonly SolidColorBrush StatusBrushIdle = new(Color.FromArgb(0xFF, 0x6B, 0x72, 0x7C));
        private static readonly SolidColorBrush StatusBrushActive = new(Color.FromArgb(0xFF, 0x1A, 0x6E, 0xC8));
        private static readonly SolidColorBrush StatusBrushSuccess = new(Color.FromArgb(0xFF, 0x12, 0x78, 0x43));
        private static readonly SolidColorBrush StatusBrushError = new(Color.FromArgb(0xFF, 0xB4, 0x23, 0x18));

        public MainPage()
        {
            InitializeComponent();

            m_mainWindow = MainWindow.CurrentWindow;

            m_mainWindow.HashUiEvents.JobPreparingHandler += HashUiEvents_JobPreparingHandler;
            m_mainWindow.HashUiEvents.JobPreparationFinishedHandler += HashUiEvents_JobPreparationFinishedHandler;
            m_mainWindow.HashUiEvents.JobCancelledHandler += HashUiEvents_JobCancelledHandler;
            m_mainWindow.HashUiEvents.JobCompletedHandler += HashUiEvents_JobCompletedHandler;
            m_mainWindow.HashUiEvents.FileStartedHandler += HashUiEvents_FileStartedHandler;
            m_mainWindow.HashUiEvents.FileMetadataHandler += HashUiEvents_FileMetadataHandler;
            m_mainWindow.HashUiEvents.FileHashHandler += HashUiEvents_FileHashHandler;
            m_mainWindow.HashUiEvents.FileErrorHandler += HashUiEvents_FileErrorHandler;
            m_mainWindow.HashUiEvents.TotalProgressHandler += HashUiEvents_TotalProgressHandler;

            m_mainWindow.IsAbleToCalc = IsAbleToCalcFiles;
            m_mainWindow.IsCalculating = IsCalculating;

            m_mainWindow.RedirectedEventHandler += OnRedirected;
            m_mainWindow.OnCloseStopEventHandler += () => StopHashCalc(true);
            m_mainWindow.OnDropFilesEventHandler += StartHashCalc;

            InitLayout();
        }

        private void InitLayout()
        {
            InitDialogFind();
            InitMenuFlyoutTextMain();
            InitRuntimeStatusTimer();
            ListViewFileTasks.ItemsSource = m_fileTaskItems;
            RefreshTaskListVisibility();
            UpdateStatusSummary();
            UpdateCommandState();
        }

        private void InitRuntimeStatusTimer()
        {
            m_runtimeStatusTimer = DispatcherQueue.CreateTimer();
            m_runtimeStatusTimer.Interval = TimeSpan.FromMilliseconds(250);
            m_runtimeStatusTimer.IsRepeating = true;
            m_runtimeStatusTimer.Tick += (_, _) =>
            {
                if (!IsCalculating())
                {
                    return;
                }

                UpdateRuntimeStatus();
            };
            m_runtimeStatusTimer.Start();
        }

        private void InitDialogFind()
        {
            m_textBoxFindHash = new()
            {
                Height = (double)Application.Current.Resources["TextControlThemeMinHeight"],
                Width = 400,
                PlaceholderText = m_resourceLoaderMain.GetString("HashValue")
            };
            m_dialogFind = new()
            {
                XamlRoot = m_mainWindow.Content.XamlRoot,
                Title = m_resourceLoaderMain.GetString("FindDialogTitle"),
                // MaxWidth = ActualWidth,
                PrimaryButtonText = "OK",
                SecondaryButtonText = "Cancel",
                Content = m_textBoxFindHash,
                DefaultButton = ContentDialogButton.Primary
            };
        }

        private void InitMenuFlyoutTextMain()
        {
            m_menuFlyoutTextMain = new()
            {
                XamlRoot = m_mainWindow.Content.XamlRoot
            };

            MenuFlyoutItem menuItemCopy = new();
            menuItemCopy.Text = m_resourceLoaderMain.GetString("MenuItemCopy");
            menuItemCopy.Click += MenuItemCopy_Click;

            m_menuFlyoutTextMain.Items.Add(menuItemCopy);
        }

        private void RefreshTaskListVisibility()
        {
            if (PanelTaskEmptyState != null)
            {
                PanelTaskEmptyState.Visibility = m_fileTaskItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateCommandState()
        {
            bool isCalculating = IsCalculating();
            bool hasVisibleResult =
                m_mainWindow.HashMgmt.GetResultCount() > 0 ||
                m_mainPageStat == MainPageControlStat.MainPageVerify;

            ButtonOpenFolder.IsEnabled = !isCalculating;
            ButtonVerify.IsEnabled = !isCalculating && m_mainWindow.HashMgmt.GetResultCount() > 0;
            ButtonCopy.IsEnabled = !isCalculating && hasVisibleResult;
            ButtonExport.IsEnabled = !isCalculating && hasVisibleResult;
            ButtonSettings.IsEnabled = true;
        }

        private void UpdateStatusSummary()
        {
            int totalFiles = m_fileTaskItems.Count;
            int completedFiles = m_fileTaskItems.Count(item => item.IsCompleted);
            int failedFiles = m_fileTaskItems.Count(item => item.IsFailed);
            int activeFiles = m_fileTaskItems.Count(item => !item.IsCompleted && !item.IsFailed);

            TextBlockStatusSummary.Text = string.Format(
                m_resourceLoaderMain.GetString("StatusSummaryTotalFormat"),
                totalFiles);
            TextBlockStatusCompleted.Text = string.Format(
                m_resourceLoaderMain.GetString("StatusSummaryCompletedFormat"),
                completedFiles);
            TextBlockStatusFailed.Text = string.Format(
                m_resourceLoaderMain.GetString("StatusSummaryFailedFormat"),
                failedFiles);
            TextBlockStatusActive.Text = string.Format(
                m_resourceLoaderMain.GetString("StatusSummaryActiveFormat"),
                activeFiles);
        }

        private void UpdateRuntimeStatus()
        {
            long endTime = IsCalculating() ? WinUIHelper.GetCurrentMilliSec() : m_calcEndTime;
            if (endTime < m_calcStartTime)
            {
                endTime = m_calcStartTime;
            }

            long elapsedMs = Math.Max(0, endTime - m_calcStartTime);
            TimeSpan elapsed = TimeSpan.FromMilliseconds(elapsedMs);
            TextBlockStatusElapsed.Text = string.Format(
                m_resourceLoaderMain.GetString("StatusSummaryElapsedFormat"),
                elapsed.ToString(@"mm\:ss"));

            string speedText = string.Empty;
            if (elapsedMs > 10 && m_totalSizeSnapshot > 0 && m_totalProgressValue > 0)
            {
                double progressRatio = (double)m_totalProgressValue / m_mainWindow.HashUiEvents.GetProgressValueMax();
                ulong processedBytes = (ulong)(m_totalSizeSnapshot * progressRatio);
                double bytesPerSecond = processedBytes / (elapsedMs / 1000.0);
                if (bytesPerSecond > 0)
                {
                    speedText = WinUIHelper.ConvertSizeToShortSizeStr((ulong)bytesPerSecond, true);
                    if (!string.IsNullOrEmpty(speedText))
                    {
                        speedText += "/s";
                    }
                }
            }

            TextBlockSpeed.Text = string.IsNullOrWhiteSpace(speedText)
                ? m_resourceLoaderMain.GetString("StatusSummarySpeedEmpty")
                : speedText;
        }

        private void ResetTaskView()
        {
            m_fileTaskIndex.Clear();
            m_fileTaskItems.Clear();
            m_totalProgressValue = 0;
            m_totalSizeSnapshot = 0;
            RefreshTaskListVisibility();
            UpdateStatusSummary();
            UpdateRuntimeStatus();
        }

        private static string GetFileDisplayName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string fileName = Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
        }

        private string GetCurrentVisibleText()
        {
            if (m_paragraphMain == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            foreach (Inline inline in m_paragraphMain.Inlines)
            {
                AppendInlineText(builder, inline);
            }

            return builder.ToString();
        }

        private bool HasVisibleResultContent()
        {
            return !string.IsNullOrWhiteSpace(GetCurrentVisibleText());
        }

        private static void AppendInlineText(StringBuilder builder, Inline inline)
        {
            switch (inline)
            {
                case Run run:
                    builder.Append(run.Text);
                    break;
                case Hyperlink hyperlink:
                    foreach (Inline innerInline in hyperlink.Inlines)
                    {
                        AppendInlineText(builder, innerInline);
                    }
                    break;
                case Span span:
                    foreach (Inline innerInline in span.Inlines)
                    {
                        AppendInlineText(builder, innerInline);
                    }
                    break;
                case LineBreak:
                    builder.AppendLine();
                    break;
            }
        }

        private FileTaskProgressItem GetOrCreateTaskItem(HashResultNet hashResult)
        {
            string key = hashResult.Path ?? string.Empty;
            if (!m_fileTaskIndex.TryGetValue(key, out FileTaskProgressItem taskItem))
            {
                taskItem = new()
                {
                    FilePath = key,
                    FileName = GetFileDisplayName(key),
                    AlgorithmText = m_selectedAlgorithmsSummary,
                    StatusText = m_resourceLoaderMain.GetString("TaskStatusPending"),
                    StatusBrush = StatusBrushIdle,
                    ProgressText = "0%",
                    IsIndeterminate = true,
                    ProgressValue = 0
                };
                m_fileTaskIndex[key] = taskItem;
                m_fileTaskItems.Add(taskItem);
                RefreshTaskListVisibility();
                UpdateStatusSummary();
            }

            return taskItem;
        }

        private void UpdateTaskItemStatus(FileTaskProgressItem taskItem, string statusText, Brush statusBrush)
        {
            taskItem.StatusText = statusText;
            taskItem.StatusBrush = statusBrush;
        }

        private void UpdateTaskProgressEstimate()
        {
            if (m_fileTaskItems.Count == 0 || m_totalSizeSnapshot == 0)
            {
                return;
            }

            int progressMax = Math.Max(1, m_mainWindow.HashUiEvents.GetProgressValueMax());
            double ratio = Math.Clamp((double)m_totalProgressValue / progressMax, 0, 1);
            ulong processedBytes = (ulong)(m_totalSizeSnapshot * ratio);
            ulong completedBytes = 0;

            foreach (FileTaskProgressItem taskItem in m_fileTaskItems)
            {
                if (taskItem.IsCompleted)
                {
                    completedBytes += taskItem.FileSize;
                    taskItem.IsIndeterminate = false;
                    taskItem.ProgressValue = 100;
                    taskItem.ProgressText = "100%";
                    continue;
                }

                if (taskItem.IsFailed)
                {
                    taskItem.IsIndeterminate = false;
                    taskItem.ProgressValue = 0;
                    taskItem.ProgressText = "ERR";
                    continue;
                }

                if (taskItem.FileSize == 0)
                {
                    taskItem.IsIndeterminate = true;
                    taskItem.ProgressText = m_resourceLoaderMain.GetString("TaskProgressWorking");
                    break;
                }

                ulong bytesIntoCurrent = processedBytes > completedBytes ? processedBytes - completedBytes : 0;
                double progress = Math.Clamp((double)bytesIntoCurrent / taskItem.FileSize, 0, 1);
                taskItem.IsIndeterminate = false;
                taskItem.ProgressValue = progress * 100.0;
                taskItem.ProgressText = string.Format("{0:0}%", taskItem.ProgressValue);
                break;
            }
        }

        private void ShowAboutPage()
        {
            Frame.Navigate(typeof(AboutPage));
        }

        private void CloseAboutPage()
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private void BringWindowToFront()
        {
            DispatcherQueue.TryEnqueue(() => Win32Helper.SetForegroundWindow(m_mainWindow.HWNDHandle));
        }

        private void ScrollTextMainToBottom()
        {
            if (m_pageLoaded)
                WinUIHelper.ScrollViewerToBottom(ScrollViewerMain);
            else
                m_pendingScrollToBottom = true;
        }

        private Paragraph CreateParagraphForTextMain()
        {
            Paragraph paragraph = new()
            {
                FontFamily = new("Consolas"),
                LineHeight = 18,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight
            };
            return paragraph;
        }

        private Hyperlink GenHyperlinkFromStringForRichTextMain(string strContent)
        {
            return WinUIHelper.GenHyperlinkFromString(strContent, RichTextMainHyperlink_Click);
        }

        private void AppendInlinesToTextMain(List<Inline> inlines, bool scrollBottom = true)
        {
            if (inlines != null)
            {
                foreach (Inline inline in inlines)
                {
                    m_paragraphMain.Inlines.Add(inline);
                }
            }
            if (scrollBottom)
            {
                ScrollTextMainToBottom();
            }
        }

        private void AppendInlineToTextMain(Inline inline)
        {
            List<Inline> inlines = [inline];
            AppendInlinesToTextMain(inlines);
        }

        private bool CanUpdateTextMain()
        {
            if (!IsCalculating())
            {
                return true;
            }

            if (m_inMainQueue < 100)
            {
                return (m_inMainQueue - m_outMainQueue < m_maxDiffQueue);
            }
            else
            {
                return (m_inlinesQueue.Count > (m_inMainQueue / 2));
            }
        }

        private void AppendInlinesQueueToTextMain()
        {
            if (m_inlinesQueue.Count > 0)
            {
                AppendInlinesToTextMain(m_inlinesQueue);
                m_inlinesQueue.Clear();
            }
        }

        private void ClearTextMain()
        {
            m_paragraphMain.Inlines.Clear();
        }

        private void SetPageControlStat(MainPageControlStat newStat)
        {
            switch (newStat)
            {
                case MainPageControlStat.MainPageNone:
                case MainPageControlStat.MainPageCalcFinish:
                    // MainPageControlStat.MainPageNone
                    if (newStat == MainPageControlStat.MainPageNone)
                    {
                        m_hyperlinksMain.Clear();
                        m_hyperlinksResult.Clear();
                        m_hyperlinksFind.Clear();
                        m_mainWindow.HashMgmt.Clear();
                        m_calcStartTime = 0;
                        m_calcEndTime = 0;
                        m_totalProgressValue = 0;
                        m_totalSizeSnapshot = 0;
                        m_mainWindow.SetTaskbarProgress(0);
                        ResetTaskView();

                        Span spanInit = new();
                        string strPageInit = m_resourceLoaderMain.GetString("MainPageInitInfo");
                        spanInit.Inlines.Add(WinUIHelper.GenRunFromString(strPageInit));
                        spanInit.Inlines.Add(WinUIHelper.GenRunFromString("\r\n"));
                        ClearTextMain();
                        AppendInlineToTextMain(spanInit);
                    }
                    // Passthrough to MainPageControlStat.MainPageCalcFinish
                    if (newStat != MainPageControlStat.MainPageNone)
                    {
                        m_calcEndTime = WinUIHelper.GetCurrentMilliSec();
                    }

                    ButtonOpen.Content = m_resourceLoaderMain.GetString("ButtonOpenOpen");
                    CheckBoxUppercase.IsEnabled = true;
                    SetHashAlgorithmControlsEnabled(true);
                    break;
                case MainPageControlStat.MainPageCalcIng:
                    CloseAboutPage();
                    SplitViewMain.IsPaneOpen = false;

                    m_calcStartTime = WinUIHelper.GetCurrentMilliSec();
                    m_mainWindow.HashMgmt.SetStop(false);
                    m_calcEndTime = m_calcStartTime;
                    m_totalProgressValue = 0;
                    TextBlockSpeed.Text = m_resourceLoaderMain.GetString("StatusSummarySpeedEmpty");
                    ButtonOpen.Content = m_resourceLoaderMain.GetString("ButtonOpenStop");
                    CheckBoxUppercase.IsEnabled = false;
                    SetHashAlgorithmControlsEnabled(false);

                    BringWindowToFront();
                    break;
                case MainPageControlStat.MainPageVerify:
                    break;
            }

            MainPageControlStat oldStat = m_mainPageStat;
            m_mainPageStat = newStat;
            UpdateRuntimeStatus();
            UpdateCommandState();

            if (oldStat == MainPageControlStat.MainPageWaitingExit &&
                m_mainPageStat == MainPageControlStat.MainPageCalcFinish)
            {
                // Wait to close
                DispatcherQueue.TryEnqueue(m_mainWindow.Close);
            }
        }

        private void UpdateUppercaseStat(bool saveLocalSetting = true)
        {
            bool? uppercaseIsChecked = CheckBoxUppercase.IsChecked;
            if (uppercaseIsChecked.HasValue && uppercaseIsChecked.Value)
            {
                m_uppercaseChecked = true;
            }
            else
            {
                m_uppercaseChecked = false;
            }
            if (saveLocalSetting)
            {
                WinUIHelper.SaveLocalSettings(KeyUppercase, m_uppercaseChecked);
            }
        }

        private static bool GetCheckBoxValue(CheckBox checkBox)
        {
            return checkBox.IsChecked.HasValue && checkBox.IsChecked.Value;
        }

        private static string GetHashAlgorithmSettingKey(HashAlgorithmDescriptorNet hashAlgorithm)
        {
            return KeyHashAlgorithmPrefix + GetHashAlgorithmId(hashAlgorithm);
        }

        private static string GetHashAlgorithmId(HashAlgorithmDescriptorNet hashAlgorithm)
        {
            return string.IsNullOrWhiteSpace(hashAlgorithm.AlgorithmId) ? hashAlgorithm.StableName : hashAlgorithm.AlgorithmId;
        }

        private void SetHashAlgorithmControlsEnabled(bool enabled)
        {
            foreach (CheckBox checkBox in m_hashAlgorithmCheckBoxes.Values)
            {
                checkBox.IsEnabled = enabled;
            }
        }

        private bool IsAnyHashAlgorithmSelected()
        {
            foreach (CheckBox checkBox in m_hashAlgorithmCheckBoxes.Values)
            {
                if (GetCheckBoxValue(checkBox))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateHashAlgorithmStat(bool saveLocalSetting = true)
        {
            m_mainWindow.HashMgmt.ResetHashAlgorithms();
            List<string> selectedLabels = [];
            foreach (HashAlgorithmDescriptorNet hashAlgorithm in m_hashAlgorithms)
            {
                if (!m_hashAlgorithmCheckBoxes.TryGetValue(GetHashAlgorithmId(hashAlgorithm), out CheckBox checkBox))
                {
                    continue;
                }

                bool hashAlgorithmEnabled = GetCheckBoxValue(checkBox);
                if (saveLocalSetting)
                {
                    WinUIHelper.SaveLocalSettings(GetHashAlgorithmSettingKey(hashAlgorithm), hashAlgorithmEnabled);
                }

                m_mainWindow.HashMgmt.SetHashAlgorithmEnabledById(GetHashAlgorithmId(hashAlgorithm), hashAlgorithmEnabled);
                if (hashAlgorithmEnabled)
                {
                    selectedLabels.Add(hashAlgorithm.DisplayLabel);
                }
            }

            m_selectedAlgorithmsSummary = selectedLabels.Count > 0
                ? string.Join(" · ", selectedLabels)
                : m_resourceLoaderMain.GetString("TaskAlgorithmNone");

            foreach (FileTaskProgressItem taskItem in m_fileTaskItems.Where(item => !item.IsCompleted && !item.IsFailed))
            {
                taskItem.AlgorithmText = m_selectedAlgorithmsSummary;
            }
        }

        private void LoadHashAlgorithmControls()
        {
            StackPanelHashAlgorithms.Children.Clear();
            m_hashAlgorithmCheckBoxes.Clear();
            m_hashAlgorithms = m_mainWindow.HashMgmt.GetSupportedHashAlgorithms();

            int tabIndex = 4;
            foreach (HashAlgorithmDescriptorNet hashAlgorithm in m_hashAlgorithms)
            {
                CheckBox checkBox = new()
                {
                    Content = hashAlgorithm.DisplayLabel,
                    IsChecked = (bool)(WinUIHelper.LoadLocalSettings(GetHashAlgorithmSettingKey(hashAlgorithm)) ?? true),
                    TabIndex = tabIndex++
                };
                checkBox.Checked += CheckBoxHashAlgorithm_Checked;
                checkBox.Unchecked += CheckBoxHashAlgorithm_Unchecked;

                StackPanelHashAlgorithms.Children.Add(checkBox);
                m_hashAlgorithmCheckBoxes[GetHashAlgorithmId(hashAlgorithm)] = checkBox;
            }
        }

        private async Task<bool> ValidateHashAlgorithmSelectionAsync()
        {
            if (IsAnyHashAlgorithmSelected())
            {
                return true;
            }

            ContentDialog dialog = new()
            {
                XamlRoot = m_mainWindow.Content.XamlRoot,
                Title = m_resourceLoaderMain.GetString("HashAlgorithmDialogTitle"),
                Content = m_resourceLoaderMain.GetString("HashAlgorithmDialogMessage"),
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close
            };
            await dialog.ShowAsync();
            return false;
        }

        private void UpdateResultUppercase()
        {
            // Refresh stat
            UpdateUppercaseStat();

            // Refresh result & find
            List<List<Hyperlink>> hyperlinkLists = [];
            hyperlinkLists.Add(m_hyperlinksResult);
            hyperlinkLists.Add(m_hyperlinksFind);
            foreach (List<Hyperlink> hyperlinkListItr in hyperlinkLists)
            {
                foreach (Hyperlink hyperlink in hyperlinkListItr)
                {
                    if (hyperlink.Inlines.Count == 0)
                        continue;

                    string hyperLinkText = WinUIHelper.GetTextFromHyperlink(hyperlink);
                    if (m_uppercaseChecked)
                        hyperLinkText = hyperLinkText.ToUpper();
                    else
                        hyperLinkText = hyperLinkText.ToLower();

                    Run runInHyperlink = (Run)hyperlink.Inlines[0];
                    runInHyperlink.Text = hyperLinkText;
                }
            }
        }

        private bool IsAbleToCalcFiles()
        {
            return !IsCalculating();
        }

        private bool IsCalculating()
        {
            return (m_mainPageStat == MainPageControlStat.MainPageCalcIng ||
                m_mainPageStat == MainPageControlStat.MainPageWaitingExit);
        }

        private async void StartHashCalc(List<string> filePaths)
        {
            if (!IsAbleToCalcFiles())
            {
                return;
            }

            if (!await ValidateHashAlgorithmSelectionAsync())
            {
                return;
            }

            if (m_mainPageStat == MainPageControlStat.MainPageVerify ||
                m_mainWindow.HashMgmt.GetResultCount() > 0 ||
                m_fileTaskItems.Count > 0)
            {
                SetPageControlStat(MainPageControlStat.MainPageNone);
            }

            m_mainWindow.HashMgmt.AddFiles(filePaths.ToArray());

            UpdateUppercaseStat();
            UpdateHashAlgorithmStat();
            m_mainWindow.HashMgmt.SetUppercase(m_uppercaseChecked);

            ResetTaskView();
            m_totalSizeSnapshot = m_mainWindow.HashMgmt.GetTotalSize();
            m_mainWindow.SetTaskbarProgress(1);

            SetPageControlStat(MainPageControlStat.MainPageCalcIng);
            ClearTextMain();

            // Ready to go
            m_inMainQueue = 0;
            m_outMainQueue = 0;
            m_inlinesQueue.Clear();
            m_mainWindow.HashMgmt.StartHashThread();
        }

        private void StopHashCalc(bool needExit)
        {
            if (m_mainPageStat == MainPageControlStat.MainPageCalcIng)
            {
                m_mainWindow.HashMgmt.SetStop(true);

                if (needExit)
                {
                    SetPageControlStat(MainPageControlStat.MainPageWaitingExit);
                }
            }
        }

        private void CalculateFinished()
        {
            AppendInlinesQueueToTextMain();

            SetPageControlStat(MainPageControlStat.MainPageCalcFinish);

            int progMax = m_mainWindow.HashUiEvents.GetProgressValueMax();
            m_totalProgressValue = progMax;
            UpdateTaskProgressEstimate();
            m_mainWindow.SetTaskbarProgress((ulong)progMax);
            UpdateRuntimeStatus();
        }

        private void CalculateStopped()
        {
            AppendInlinesQueueToTextMain();
            AppendInlineToTextMain(WinUIHelper.GenRunFromString("\r\n"));

            SetPageControlStat(MainPageControlStat.MainPageCalcFinish);
            m_totalProgressValue = 0;
            m_mainWindow.SetTaskbarProgress(0);
            UpdateRuntimeStatus();
        }

        private void AppendFileNameToTextMain(HashResultNet hashResult)
        {
            m_outMainQueue += 1;
            string strAppend = m_resourceLoaderMain.GetString("ResultFileName");
            strAppend += " ";
            strAppend += hashResult.Path;
            m_inlinesQueue.Add(WinUIHelper.GenRunFromString(strAppend));
            m_inlinesQueue.Add(WinUIHelper.GenRunFromString("\r\n"));

            if (CanUpdateTextMain())
            {
                AppendInlinesQueueToTextMain();
            }
        }

        private void AppendFileMetaToTextMain(HashResultNet hashResult)
        {
            m_outMainQueue += 1;
            string strShortSize = WinUIHelper.ConvertSizeToShortSizeStr(hashResult.Size);
            string strSize = m_resourceLoaderMain.GetString("ResultFileSize");
            strSize += " ";
            strSize += hashResult.Size;
            strSize += " ";
            strSize += m_resourceLoaderMain.GetString("ResultByte");
            if (!string.IsNullOrEmpty(strShortSize))
            {
                strSize += " (";
                strSize += strShortSize;
                strSize += ")";
            }
            string strModifiedTime = m_resourceLoaderMain.GetString("ResultModifiedTime");
            strModifiedTime += " ";
            strModifiedTime += hashResult.ModifiedDate;
            m_inlinesQueue.Add(WinUIHelper.GenRunFromString(strSize));
            m_inlinesQueue.Add(WinUIHelper.GenRunFromString("\r\n"));
            m_inlinesQueue.Add(WinUIHelper.GenRunFromString(strModifiedTime));
            m_inlinesQueue.Add(WinUIHelper.GenRunFromString("\r\n"));
            if (!string.IsNullOrEmpty(hashResult.Version))
            {
                string strVersion = m_resourceLoaderMain.GetString("ResultFileVersion");
                strVersion += " ";
                strVersion += hashResult.Version;
                m_inlinesQueue.Add(WinUIHelper.GenRunFromString(strVersion));
                m_inlinesQueue.Add(WinUIHelper.GenRunFromString("\r\n"));
            }

            if (CanUpdateTextMain())
            {
                AppendInlinesQueueToTextMain();
            }
        }

        private void AppendDigestHashToTextMain(List<Inline> inlines, string digestLabel, string digestValue)
        {
            if (string.IsNullOrEmpty(digestValue))
            {
                return;
            }

            inlines.Add(WinUIHelper.GenRunFromString(digestLabel));
            Hyperlink hyperlinkDigest = GenHyperlinkFromStringForRichTextMain(digestValue);
            m_hyperlinksMain.Add(hyperlinkDigest);
            inlines.Add(hyperlinkDigest);
            inlines.Add(WinUIHelper.GenRunFromString("\r\n"));
        }

        private void AppendFileHashToTextMain(HashResultNet hashResult, bool uppercase)
        {
            m_outMainQueue += 1;
            string strFileMD5, strFileSHA1, strFileSHA256, strFileSHA512;

            if (uppercase)
            {
                strFileMD5 = hashResult.MD5.ToUpper();
                strFileSHA1 = hashResult.SHA1.ToUpper();
                strFileSHA256 = hashResult.SHA256.ToUpper();
                strFileSHA512 = hashResult.SHA512.ToUpper();
            }
            else
            {
                strFileMD5 = hashResult.MD5.ToLower();
                strFileSHA1 = hashResult.SHA1.ToLower();
                strFileSHA256 = hashResult.SHA256.ToLower();
                strFileSHA512 = hashResult.SHA512.ToLower();
            }

            int inlineCountBefore = m_inlinesQueue.Count;
            AppendDigestHashToTextMain(m_inlinesQueue, "MD5: ", strFileMD5);
            AppendDigestHashToTextMain(m_inlinesQueue, "SHA1: ", strFileSHA1);
            AppendDigestHashToTextMain(m_inlinesQueue, "SHA256: ", strFileSHA256);
            AppendDigestHashToTextMain(m_inlinesQueue, "SHA512: ", strFileSHA512);
            if (m_inlinesQueue.Count > inlineCountBefore)
            {
                m_inlinesQueue.Add(WinUIHelper.GenRunFromString("\r\n"));
            }

            if (CanUpdateTextMain())
            {
                AppendInlinesQueueToTextMain();
            }
        }

        private void AppendFileErrToTextMain(HashResultNet hashResult)
        {
            m_outMainQueue += 1;
            string strAppend = hashResult.Error;
            m_inlinesQueue.Add(WinUIHelper.GenRunFromString(strAppend));
            m_inlinesQueue.Add(WinUIHelper.GenRunFromString("\r\n\r\n"));

            if (CanUpdateTextMain())
            {
                AppendInlinesQueueToTextMain();
            }
        }

        private void AppendFileResultToTextMain(HashResultNet hashResult, bool uppercase)
        {
            if (hashResult.EnumState == HashResultStateNet.ResultNone)
            {
                return;
            }

            if (hashResult.EnumState == HashResultStateNet.ResultAll ||
                hashResult.EnumState == HashResultStateNet.ResultMeta ||
                hashResult.EnumState == HashResultStateNet.ResultError ||
                hashResult.EnumState == HashResultStateNet.ResultPath)
            {
                AppendFileNameToTextMain(hashResult);
            }

            if (hashResult.EnumState == HashResultStateNet.ResultAll ||
                hashResult.EnumState == HashResultStateNet.ResultMeta)
            {
                AppendFileMetaToTextMain(hashResult);
            }

            if (hashResult.EnumState == HashResultStateNet.ResultAll)
            {
                AppendFileHashToTextMain(hashResult, uppercase);
            }

            if (hashResult.EnumState == HashResultStateNet.ResultError)
            {
                AppendFileErrToTextMain(hashResult);
            }

            if (hashResult.EnumState != HashResultStateNet.ResultAll &&
                hashResult.EnumState != HashResultStateNet.ResultError)
            {
                AppendInlineToTextMain(WinUIHelper.GenRunFromString("\r\n"));
            }
        }

        private async void ShowFindDialog()
        {
            m_textBoxFindHash.Text = "";
            ContentDialogResult result = await m_dialogFind.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string strHashToFind = m_textBoxFindHash.Text;
                HashResultNet[] hashResultNetArray = m_mainWindow.HashMgmt.FindHashResults(strHashToFind);
                DispatcherQueue.TryEnqueue(() => ShowFindResult(strHashToFind, hashResultNetArray));
            }
        }

        private void ShowFindResult(string strHashToFind, HashResultNet[] hashResultNetArray)
        {
            // Fix strange behavior
            ScrollViewerMain.ChangeView(null, 0.0, null, true);
            ScrollViewerMain.ChangeView(0.01, null, null); // WTF?
            ScrollViewerMain.IsEnabled = false;

            // Switch m_paragraphMain
            RichTextMain.Blocks.Clear();
            m_paragraphMain = m_paragraphFind;
            RichTextMain.Blocks.Add(m_paragraphMain);
            m_hyperlinksMain = m_hyperlinksFind;

            // Show result
            List<Inline> inlines = [];
            string strFindResult = m_resourceLoaderMain.GetString("FindResultTitle");
            inlines.Add(WinUIHelper.GenRunFromString(strFindResult));
            inlines.Add(WinUIHelper.GenRunFromString("\r\n"));
            string strHashValue = m_resourceLoaderMain.GetString("HashValue");
            strHashValue += ": ";
            inlines.Add(WinUIHelper.GenRunFromString(strHashValue));
            inlines.Add(WinUIHelper.GenRunFromString(strHashToFind));
            inlines.Add(WinUIHelper.GenRunFromString("\r\n"));
            string strFindResultBegin = m_resourceLoaderMain.GetString("FindResultBegin");
            inlines.Add(WinUIHelper.GenRunFromString(strFindResultBegin));
            inlines.Add(WinUIHelper.GenRunFromString("\r\n\r\n"));
            AppendInlinesToTextMain(inlines);

            if (hashResultNetArray == null || hashResultNetArray.Length == 0)
            {
                // No match
                List<Inline> inlinesResult = [];
                string strFindNoResult = m_resourceLoaderMain.GetString("FindNoResult");
                inlinesResult.Add(WinUIHelper.GenRunFromString(strFindNoResult));
                inlinesResult.Add(WinUIHelper.GenRunFromString("\r\n"));
                AppendInlinesToTextMain(inlinesResult);
            }
            else
            {
                // Found some
                foreach (HashResultNet hashResult in hashResultNetArray)
                {
                    AppendFileResultToTextMain(hashResult, m_uppercaseChecked);
                }
            }

            SetPageControlStat(MainPageControlStat.MainPageVerify);

            // Fix strange behavior
            ScrollViewerMain.ChangeView(0.0, null, null);
            ScrollViewerMain.IsEnabled = true;
            ScrollTextMainToBottom();
        }

        private void ClearFindResult()
        {
            // Switch m_paragraphMain
            RichTextMain.Blocks.Clear();
            m_paragraphMain = m_paragraphResult;
            RichTextMain.Blocks.Add(m_paragraphMain);
            ScrollTextMainToBottom();
            m_hyperlinksMain = m_hyperlinksResult;

            if (m_mainWindow.HashMgmt.GetResultCount() > 0)
            {
                SetPageControlStat(MainPageControlStat.MainPageCalcFinish);
            }
            else
            {
                SetPageControlStat(MainPageControlStat.MainPageNone);
            }

            // Clear find result
            m_paragraphFind.Inlines.Clear();
            m_hyperlinksFind.Clear();
        }

        private void ShowRichTextMainMenuFlyout()
        {
            double scale = m_mainWindow.Scale;
            int menuOffsetX = 4;
            int menuOffsetY = 2;
            System.Drawing.Point sdPointCursor = m_mainWindow.GetCursorRelativePoint();
            Windows.Foundation.Point wfPointCuror = new((sdPointCursor.X / scale) + menuOffsetX, (sdPointCursor.Y / scale) + menuOffsetY);
            m_menuFlyoutTextMain?.ShowAt(null, wfPointCuror);
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            if (m_hyperlinkClicked == null)
                return;

            string strHash = WinUIHelper.GetTextFromHyperlink(m_hyperlinkClicked);
            NativeHelper nativeHelper = new();
            nativeHelper.SetClipboardText(strHash);
        }

        private void HandleRichTextSelectionScroll(ScrollViewer scrollViewerWrapper)
        {
            //string strDebug = "";

            // cursor position
            double scale = m_mainWindow.Scale;
            System.Drawing.Point pointCursor = m_mainWindow.GetCursorRelativePoint();

            //strDebug = string.Format("{0:0.00} : {1:0.00}", pointCursor.X, pointCursor.Y);
            //TextBlockDebug.Text = strDebug;

            // ScrollView position
            GeneralTransform transformScrollView = scrollViewerWrapper.TransformToVisual(null);
            Windows.Foundation.Point pointScrollView = transformScrollView.TransformPoint(new(0, 0));

            // cursor offset relative to ScrollView
            double cursorRelateScrollOffX = pointCursor.X - pointScrollView.X - (scrollViewerWrapper.Margin.Left * scale);
            double cursorRelateScrollOffY = pointCursor.Y - pointScrollView.Y - (scrollViewerWrapper.Margin.Top * scale);

            double scrollViewWidth = scrollViewerWrapper.ActualWidth * scale;
            double scrollViewHeight = scrollViewerWrapper.ActualHeight * scale;

            double cursorOutScrollWidthOffX = cursorRelateScrollOffX;
            if (cursorOutScrollWidthOffX > 0 && cursorOutScrollWidthOffX <= scrollViewWidth)
            {
                // X inside
                cursorOutScrollWidthOffX = 0;
            }
            else if (cursorOutScrollWidthOffX > scrollViewWidth)
            {
                // X outside right
                cursorOutScrollWidthOffX = cursorOutScrollWidthOffX - scrollViewWidth;
            }

            double cursorOutScrollHeightOffY = cursorRelateScrollOffY;
            if (cursorOutScrollHeightOffY > 0 && cursorOutScrollHeightOffY <= scrollViewHeight)
            {
                // Y inside
                cursorOutScrollHeightOffY = 0;
            }
            else if (cursorOutScrollHeightOffY > scrollViewHeight)
            {
                // Y outside right
                cursorOutScrollHeightOffY = cursorOutScrollHeightOffY - scrollViewHeight;
            }

            //strDebug = string.Format("{0:0.00} : {1:0.00}", cursorOutScrollWidthOffX, cursorOutScrollHeightOffY);

            if (cursorOutScrollWidthOffX == 0 && cursorOutScrollHeightOffY == 0)
            {
                // X and Y all inside
                //strDebug = string.Format("{0:0.00} : {1:0.00}", cursorOutScrollWidthOffX, cursorOutScrollHeightOffY);
                //TextBlockDebug.Text = strDebug;
                return;
            }

            double scrollViewCurOffX = scrollViewerWrapper.HorizontalOffset;
            double scrollViewCurOffY = scrollViewerWrapper.VerticalOffset;
            double scrollViewNewOffX = scrollViewCurOffX + cursorOutScrollWidthOffX;
            double scrollViewNewOffY = scrollViewCurOffY + cursorOutScrollHeightOffY;

            //strDebug = string.Format("{0:0.00} : {1:0.00}", scrollViewNewOffX, scrollViewNewOffY);
            WinUIHelper.ScrollViewerScrollTo(scrollViewerWrapper, scrollViewNewOffX, scrollViewNewOffY);

            //TextBlockDebug.Text = strDebug;
        }

        private void OnRedirected(string someArgs)
        {
            if (string.IsNullOrEmpty(someArgs))
                return;

            string[] splitArgs = CommandLineParser.SplitCommandLine(someArgs).ToArray();
            List<string> strFilePaths = [];
            bool foundPaths = false;
            for (int i = 0; i < splitArgs.Length; i++)
            {
                if (foundPaths)
                    strFilePaths.Add(splitArgs[i]);

                if (string.Equals(splitArgs[i], ArgPaths, StringComparison.OrdinalIgnoreCase))
                    foundPaths = true;
            }

            if (strFilePaths.Count > 0)
                StartHashCalc(strFilePaths);
        }

        private void GridMain_Loaded(object sender, RoutedEventArgs e)
        {
            if (!m_pageInited)
            {
                // Prepare RichTextMain
                RichTextMain.TextWrapping = TextWrapping.NoWrap;
                m_paragraphResult = CreateParagraphForTextMain();
                m_paragraphFind = CreateParagraphForTextMain();
                m_paragraphMain = m_paragraphResult;
                RichTextMain.Blocks.Add(m_paragraphMain);
                m_hyperlinksMain = m_hyperlinksResult;

                // Prepare controls
                ButtonOpen.Content = m_resourceLoaderMain.GetString("ButtonOpenOpen");
                TextBlockSpeed.Text = m_resourceLoaderMain.GetString("StatusSummarySpeedEmpty");

                object objUppercase = WinUIHelper.LoadLocalSettings(KeyUppercase);
                CheckBoxUppercase.IsChecked = (bool)(objUppercase ?? false);
                UpdateUppercaseStat(false);

                LoadHashAlgorithmControls();

                // Init stat
                SetPageControlStat(MainPageControlStat.MainPageNone);
                UpdateHashAlgorithmStat(false);
                UpdateCommandState();

                // Handle commandline args
                DispatcherQueue.TryEnqueue(() =>
                {
                    AppActivationArguments appActiveArgs = WinUIHelper.GetCurrentActivatedEventArgs();
                    string strAppActiveArgs = WinUIHelper.GetLaunchActivatedEventArgs(appActiveArgs);
                    OnRedirected(strAppActiveArgs);
                });

                m_pageInited = true;
            }

            m_pageLoaded = true;

            if (m_pendingScrollToBottom)
            {
                m_pendingScrollToBottom = false;
                ScrollTextMainToBottom();
            }

            // Fix for color changed.
            DispatcherQueueTimer timerScrollBar = DispatcherQueue.CreateTimer();
            timerScrollBar.Interval = TimeSpan.FromMilliseconds(300);
            timerScrollBar.IsRepeating = false;
            timerScrollBar.Tick += (timer, sender) =>
            {
                ScrollViewerMain.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                ScrollViewerMain.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            };
            timerScrollBar.Start();
        }

        private void GridMain_Unloaded(object sender, RoutedEventArgs e)
        {
            m_pageLoaded = false;
        }

        private void RichTextMainHyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            m_hyperlinkClicked = sender;
            ShowRichTextMainMenuFlyout();
        }

        private void RichTextMain_SelectionChanged(object sender, RoutedEventArgs e)
        {
            HandleRichTextSelectionScroll(ScrollViewerMain);
        }

        private void CheckBoxUppercase_Checked(object sender, RoutedEventArgs e)
        {
            UpdateResultUppercase();
        }

        private void CheckBoxUppercase_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateResultUppercase();
        }

        private void CheckBoxHashAlgorithm_Checked(object sender, RoutedEventArgs e)
        {
            UpdateHashAlgorithmStat();
        }

        private void CheckBoxHashAlgorithm_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateHashAlgorithmStat();
        }

        private void ButtonVerify_Click(object sender, RoutedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(ShowFindDialog);
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            SplitViewMain.IsPaneOpen = !SplitViewMain.IsPaneOpen;
        }

        private void ButtonAboutInSettings_Click(object sender, RoutedEventArgs e)
        {
            SplitViewMain.IsPaneOpen = false;

            // Fix for color changed
            ScrollViewerMain.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            ScrollViewerMain.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

            ShowAboutPage();
        }

        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            string text = GetCurrentVisibleText();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            WinUIHelper.CopyStringToClipboard(text);
            WinUIHelper.FlushClipboard();
        }

        private async void ButtonExport_Click(object sender, RoutedEventArgs e)
        {
            string text = GetCurrentVisibleText();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            FileSavePicker picker = new();
            InitializeWithWindow.Initialize(picker, m_mainWindow.HWNDHandle);
            picker.FileTypeChoices.Add("Text", [".txt"]);
            picker.SuggestedFileName = "LHash-Results";
            StorageFile file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                return;
            }

            await FileIO.WriteTextAsync(file, text);
        }

        private async void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            if (m_mainPageStat == MainPageControlStat.MainPageCalcIng)
            {
                StopHashCalc(false);
            }
            else
            {
                FileOpenPicker picker = new();

                // Initialize the file picker with the window handle (HWND)
                InitializeWithWindow.Initialize(picker, m_mainWindow.HWNDHandle);

                // Set options for your file picker
                picker.FileTypeFilter.Add("*");

                // Open the picker for the user to pick a file
                IReadOnlyList<StorageFile> pickFiles = await picker.PickMultipleFilesAsync();
                if (pickFiles != null)
                {
                    // Application now has read/write access to the picked file
                    List<string> strPickFilePaths = [];
                    foreach (IStorageItem storageItem in pickFiles)
                    {
                        string path = storageItem.Path;
                        if (!string.IsNullOrEmpty(path))
                            strPickFilePaths.Add(path);
                    }

                    if (strPickFilePaths.Count == 0)
                        return;

                    DispatcherQueue.TryEnqueue(() => StartHashCalc(strPickFilePaths));
                }
            }
        }

        private async void ButtonOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (IsCalculating())
            {
                return;
            }

            FolderPicker picker = new();
            InitializeWithWindow.Initialize(picker, m_mainWindow.HWNDHandle);
            picker.FileTypeFilter.Add("*");

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder == null || string.IsNullOrWhiteSpace(folder.Path))
            {
                return;
            }

            DispatcherQueue.TryEnqueue(() => StartHashCalc([folder.Path]));
        }

        private void HashUiEvents_JobPreparingHandler()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                string strPrepare = m_resourceLoaderMain.GetString("ResultWaitingStart");
                m_runPrepare = WinUIHelper.GenRunFromString(strPrepare);
                AppendInlineToTextMain(m_runPrepare);
            });
        }

        private void HashUiEvents_JobPreparationFinishedHandler()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (m_runPrepare != null)
                {
                    m_runPrepare.Text = "";
                }
            });
        }

        private void HashUiEvents_JobCancelledHandler()
        {
            DispatcherQueue.TryEnqueue(CalculateStopped);
        }

        private void HashUiEvents_JobCompletedHandler()
        {
            DispatcherQueue.TryEnqueue(CalculateFinished);
        }

        private void HashUiEvents_FileStartedHandler(HashResultNet hashResult)
        {
            m_inMainQueue += 1;
            DispatcherQueue.TryEnqueue(() =>
            {
                FileTaskProgressItem taskItem = GetOrCreateTaskItem(hashResult);
                taskItem.FileName = GetFileDisplayName(hashResult.Path);
                taskItem.AlgorithmText = m_selectedAlgorithmsSummary;
                taskItem.IsIndeterminate = true;
                taskItem.ProgressValue = 0;
                taskItem.ProgressText = m_resourceLoaderMain.GetString("TaskProgressWorking");
                UpdateTaskItemStatus(taskItem, m_resourceLoaderMain.GetString("TaskStatusRunning"), StatusBrushActive);
                UpdateStatusSummary();
                AppendFileNameToTextMain(hashResult);
                UpdateCommandState();
            });
        }

        private void HashUiEvents_FileMetadataHandler(HashResultNet hashResult)
        {
            m_inMainQueue += 1;
            DispatcherQueue.TryEnqueue(() =>
            {
                FileTaskProgressItem taskItem = GetOrCreateTaskItem(hashResult);
                taskItem.FileSize = hashResult.Size;
                taskItem.IsIndeterminate = false;
                if (taskItem.ProgressValue <= 0)
                {
                    taskItem.ProgressValue = 2;
                    taskItem.ProgressText = "2%";
                }

                UpdateTaskItemStatus(taskItem, m_resourceLoaderMain.GetString("TaskStatusHashing"), StatusBrushActive);
                AppendFileMetaToTextMain(hashResult);
                UpdateTaskProgressEstimate();
            });
        }

        private void HashUiEvents_FileHashHandler(HashResultNet hashResult, bool uppercase)
        {
            m_inMainQueue += 1;
            DispatcherQueue.TryEnqueue(() =>
            {
                FileTaskProgressItem taskItem = GetOrCreateTaskItem(hashResult);
                taskItem.IsCompleted = true;
                taskItem.IsFailed = false;
                taskItem.IsIndeterminate = false;
                taskItem.ProgressValue = 100;
                taskItem.ProgressText = "100%";
                taskItem.FileSize = taskItem.FileSize == 0 ? hashResult.Size : taskItem.FileSize;
                UpdateTaskItemStatus(taskItem, m_resourceLoaderMain.GetString("TaskStatusCompleted"), StatusBrushSuccess);
                AppendFileHashToTextMain(hashResult, uppercase);
                UpdateStatusSummary();
                UpdateCommandState();
            });
        }

        private void HashUiEvents_FileErrorHandler(HashResultNet hashResult)
        {
            m_inMainQueue += 1;
            DispatcherQueue.TryEnqueue(() =>
            {
                FileTaskProgressItem taskItem = GetOrCreateTaskItem(hashResult);
                taskItem.IsCompleted = false;
                taskItem.IsFailed = true;
                taskItem.IsIndeterminate = false;
                taskItem.ProgressValue = 0;
                taskItem.ProgressText = "ERR";
                UpdateTaskItemStatus(taskItem, m_resourceLoaderMain.GetString("TaskStatusFailed"), StatusBrushError);
                AppendFileErrToTextMain(hashResult);
                UpdateStatusSummary();
                UpdateCommandState();
            });
        }

        private void HashUiEvents_TotalProgressHandler(int value)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (m_totalProgressValue == value)
                    return;

                m_totalProgressValue = value;
                UpdateTaskProgressEstimate();
                UpdateRuntimeStatus();
                if (value == 0)
                    value = 1;
                m_mainWindow.SetTaskbarProgress((ulong)value);
            });
        }
    }
}
