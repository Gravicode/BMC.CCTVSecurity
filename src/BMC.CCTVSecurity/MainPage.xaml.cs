using BMC.CCTVSecurity.Helpers;
using FrameSourceHelper_UWP;
using Microsoft.AI.Skills.SkillInterfacePreview;
using Microsoft.AI.Skills.Vision.ObjectDetectorPreview;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Media.SpeechSynthesis;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BMC.CCTVSecurity
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IFrameSource m_frameSource = null;
        private SpeechHelper speech;
        // Vision Skills
        private ObjectDetectorDescriptor m_descriptor = null;
        private ObjectDetectorBinding m_binding = null;
        private ObjectDetectorSkill m_skill = null;
        private IReadOnlyList<ISkillExecutionDevice> m_availableExecutionDevices = null;
        bool IsPlaying = false;
        // Misc
        private BoundingBoxRenderer[] m_bboxRenderer = new BoundingBoxRenderer[DataConfig.CCTVCount];
        private HashSet<ObjectKind> m_objectKinds = null;

        // Frames
        private SoftwareBitmapSource[] m_processedBitmapSource = new SoftwareBitmapSource[DataConfig.CCTVCount];
        private Random Rnd = new Random();
        // Performance metrics
        private Stopwatch m_evalStopwatch = new Stopwatch();
        private float m_bindTime = 0;
        private float m_evalTime = 0;
        private Stopwatch m_renderStopwatch = new Stopwatch();
        private static List<string> Sounds = new List<string>();
        // Locks
        private SemaphoreSlim m_lock = new SemaphoreSlim(1);
        HttpClient client = new HttpClient();
        public MainPage()
        {
            this.InitializeComponent();
        }

        async void PlaySound(string SoundFile)
        {
            if (IsPlaying) return;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
               
               
                    IsPlaying = true;
                    MediaElement mysong = speechMediaElement;// new MediaElement();
                    Windows.Storage.StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
                    Windows.Storage.StorageFile file = await folder.GetFileAsync(SoundFile);
                    var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    mysong.SetSource(stream, file.ContentType);
                    mysong.Play();
            
                    //UI code here
               
            });
        }

        private void Mysong_MediaEnded(object sender, RoutedEventArgs e)
        {
           
            IsPlaying = false;
        }

        /// <summary>
        /// Triggered when media element used to play synthesized speech messages is loaded.
        /// Initializes SpeechHelper and greets user.
        /// </summary>
        private void speechMediaElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (speech == null)
            {
                speech = new SpeechHelper(speechMediaElement);
                speechMediaElement.MediaEnded += Mysong_MediaEnded;
            }
            else
            {
                // Prevents media element from re-greeting visitor
                speechMediaElement.AutoPlay = false;
            }
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Sounds.Count <= 0)
            {
                Sounds.Add("wengi.mp3");
                Sounds.Add("setan.wav");
                Sounds.Add("setan2.wav");
                Sounds.Add("zombie.wav");
                Sounds.Add("zombie2.wav");
                Sounds.Add("scream.mp3");
                Sounds.Add("monster.mp3");

            }

            m_processedBitmapSource[0] = new SoftwareBitmapSource();
            CCTV1.Source = m_processedBitmapSource[0];

            m_processedBitmapSource[1] = new SoftwareBitmapSource();
            CCTV2.Source = m_processedBitmapSource[1];

            m_processedBitmapSource[2] = new SoftwareBitmapSource();
            CCTV3.Source = m_processedBitmapSource[2];

            m_processedBitmapSource[3] = new SoftwareBitmapSource();
            CCTV4.Source = m_processedBitmapSource[3];
            // Initialize helper class used to render the skill results on screen
            m_bboxRenderer[0] = new BoundingBoxRenderer(UIOverlayCanvas1);
            m_bboxRenderer[1] = new BoundingBoxRenderer(UIOverlayCanvas2);
            m_bboxRenderer[2] = new BoundingBoxRenderer(UIOverlayCanvas3);
            m_bboxRenderer[3] = new BoundingBoxRenderer(UIOverlayCanvas4);

            m_lock.Wait();
            {
                NotifyUser("Initializing skill...");
                m_descriptor = new ObjectDetectorDescriptor();
                m_availableExecutionDevices = await m_descriptor.GetSupportedExecutionDevicesAsync();

                await InitializeObjectDetectorAsync();
                await UpdateSkillUIAsync();
            }
            m_lock.Release();

            // Ready to begin, enable buttons
            NotifyUser("Skill initialized. Select a media source from the top to begin.");
            Loop();
        }
        /// <summary>
        /// Initialize the ObjectDetector skill
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task InitializeObjectDetectorAsync(ISkillExecutionDevice device = null)
        {
            if (device != null)
            {
                m_skill = await m_descriptor.CreateSkillAsync(device) as ObjectDetectorSkill;
            }
            else
            {
                m_skill = await m_descriptor.CreateSkillAsync() as ObjectDetectorSkill;
            }
            m_binding = await m_skill.CreateSkillBindingAsync() as ObjectDetectorBinding;
        }
        /// <summary>
        /// Print a message to the UI
        /// </summary>
        /// <param name="message"></param>
        private void NotifyUser(String message)
        {
            if (Dispatcher.HasThreadAccess)
            {
                UIMessageTextBlock.Text = message;
            }
            else
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UIMessageTextBlock.Text = message).AsTask().Wait();
            }
        }
        /// <summary>
        /// Populate UI with skill information and options
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSkillUIAsync()
        {
            if (Dispatcher.HasThreadAccess)
            {
                // Show skill description members in UI
                UISkillName.Text = m_descriptor.Name;

                UISkillDescription.Text = $"{m_descriptor.Description}" +
                $"\n\tauthored by: {m_descriptor.Version.Author}" +
                $"\n\tpublished by: {m_descriptor.Version.Author}" +
                $"\n\tversion: {m_descriptor.Version.Major}.{m_descriptor.Version.Minor}" +
                $"\n\tunique ID: {m_descriptor.Id}";

                var inputDesc = m_descriptor.InputFeatureDescriptors[0] as SkillFeatureImageDescriptor;
                UISkillInputDescription.Text = $"\tName: {inputDesc.Name}" +
                $"\n\tDescription: {inputDesc.Description}" +
                $"\n\tType: {inputDesc.FeatureKind}" +
                $"\n\tWidth: {inputDesc.Width}" +
                $"\n\tHeight: {inputDesc.Height}" +
                $"\n\tSupportedBitmapPixelFormat: {inputDesc.SupportedBitmapPixelFormat}" +
                $"\n\tSupportedBitmapAlphaMode: {inputDesc.SupportedBitmapAlphaMode}";

                var outputDesc = m_descriptor.OutputFeatureDescriptors[0] as ObjectDetectorResultListDescriptor;
                UISkillOutputDescription1.Text = $"\tName: {outputDesc.Name}, Description: {outputDesc.Description} \n\tType: Custom";

                if (m_availableExecutionDevices.Count == 0)
                {
                    NotifyUser("No execution devices available, this skill cannot run on this device");
                }
                else
                {

                    // Display available execution devices
                    UISkillExecutionDevices.ItemsSource = m_availableExecutionDevices.Select((device) => $"{device.ExecutionDeviceKind} | {device.Name}");

                    // Set SelectedIndex to index of currently selected device
                    for (int i = 0; i < m_availableExecutionDevices.Count; i++)
                    {
                        if (m_availableExecutionDevices[i].ExecutionDeviceKind == m_binding.Device.ExecutionDeviceKind
                            && m_availableExecutionDevices[i].Name == m_binding.Device.Name)
                        {
                            UISkillExecutionDevices.SelectedIndex = i;
                            break;
                        }
                    }

                }

                // Populate ObjectKind filters list with all possible classes supported by the detector
                // Exclude Undefined label (not used by the detector) from selector list
                UIObjectKindFilters.ItemsSource = Enum.GetValues(typeof(ObjectKind)).Cast<ObjectKind>().Where(kind => kind != ObjectKind.Undefined);
            }
            else
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await UpdateSkillUIAsync());
            }
        }
        async Task Loop()
        {
            //int index = 0;
            Random rnd = new Random(Environment.TickCount);
            while (true)
            {
                //index = 0;
                for (int index = 0; index < DataConfig.CCTVCount; index++)
                //Parallel.For(0, 3, async(index) =>
                {
                    try
                    {
                        var itemUrl = DataConfig.CCTVURL[index];

                        var data = await client.GetByteArrayAsync(itemUrl + rnd.Next(100));
                        //BitmapImage bmp = new BitmapImage();
                        SoftwareBitmap outputBitmap = null;
                        using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                        {
                            await stream.WriteAsync(data.AsBuffer());
                            stream.Seek(0);
                            //await bmp.SetSourceAsync(stream);
                            //new
                            ImageEncodingProperties properties = ImageEncodingProperties.CreateJpeg();

                            var decoder = await BitmapDecoder.CreateAsync(stream);
                            outputBitmap = await decoder.GetSoftwareBitmapAsync();
                        }

                        if (outputBitmap != null)
                        {

                            //SoftwareBitmap outputBitmap = SoftwareBitmap.CreateCopyFromBuffer(data.AsBuffer(), BitmapPixelFormat.Bgra8, bmp.PixelWidth, bmp.PixelHeight, BitmapAlphaMode.Premultiplied);
                            SoftwareBitmap displayableImage = SoftwareBitmap.Convert(outputBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                            VideoFrame frame = VideoFrame.CreateWithSoftwareBitmap(displayableImage);
                            //do evaluation
                            await ExecuteFrame(frame, index);


                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
                Thread.Sleep(DataConfig.EvalInterval);
            }

        }


        async Task ExecuteFrame(VideoFrame CurFrame, int CurIndex)
        {
            await m_lock.WaitAsync();
            try
            {

                await DetectObjectsAsync(CurFrame);
                await DisplayFrameAndResultAsync(CurFrame, CurIndex);
            }
            catch (Exception ex)
            {
                NotifyUser(ex.Message);
            }
            finally
            {
                m_lock.Release();
            }
        }

        /// <summary>
        /// Triggered when the expander is expanded and collapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIExpander_Expanded(object sender, EventArgs e)
        {
            /*
            var expander = (sender as Expander);
            if (expander.IsExpanded)
            {
                UIVideoFeed.Visibility = Visibility.Collapsed;
            }
            else
            {
                UIVideoFeed.Visibility = Visibility.Visible;
            }*/
        }
        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIObjectKindFilters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await m_lock.WaitAsync();
            {
                m_objectKinds = UIObjectKindFilters.SelectedItems.Cast<ObjectKind>().ToHashSet();
            }
            m_lock.Release();
        }
        /// <summary>
        /// Triggers when a skill execution device is selected from the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UISkillExecutionDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedDevice = m_availableExecutionDevices[UISkillExecutionDevices.SelectedIndex];
            await m_lock.WaitAsync();
            {
                await InitializeObjectDetectorAsync(selectedDevice);
            }
            m_lock.Release();
            if (m_frameSource != null)
            {
                await m_frameSource.StartAsync();
            }
        }
        /// <summary>
        /// Bind and evaluate the frame with the ObjectDetector skill
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private async Task DetectObjectsAsync(VideoFrame frame)
        {
            m_evalStopwatch.Restart();

            // Bind
            await m_binding.SetInputImageAsync(frame);

            m_bindTime = (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_evalStopwatch.Restart();

            // Evaluate
            await m_skill.EvaluateAsync(m_binding);

            m_evalTime = (float)m_evalStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000f;
            m_evalStopwatch.Stop();
        }

        /// <summary>
        /// Render ObjectDetector skill results
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="objectDetections"></param>
        /// <returns></returns>
        private async Task DisplayFrameAndResultAsync(VideoFrame frame, int CCTVIndex)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {

                try
                {

                    if (frame.SoftwareBitmap != null)
                    {
                        await m_processedBitmapSource[CCTVIndex].SetBitmapAsync(frame.SoftwareBitmap);
                    }
                    else
                    {
                        var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Direct3DSurface, BitmapAlphaMode.Ignore);
                        await m_processedBitmapSource[CCTVIndex].SetBitmapAsync(bitmap);
                    }

                    // Retrieve and filter results if requested
                    IReadOnlyList<ObjectDetectorResult> objectDetections = m_binding.DetectedObjects;
                    if (m_objectKinds?.Count > 0)
                    {
                        objectDetections = objectDetections.Where(det => m_objectKinds.Contains(det.Kind)).ToList();
                    }
                    if (objectDetections != null)
                    {
                        // Update displayed results
                        m_bboxRenderer[CCTVIndex].Render(objectDetections);
                        bool PersonDetected = false;
                        int PersonCount = 0;
                        foreach(var obj in objectDetections)
                        {
                            if (obj.Kind.ToString().ToLower() == "person")
                            {
                                PersonCount++;
                                PersonDetected = true;
                            }
                        }
                        if (PersonDetected)
                        {
                            if ((bool)ChkMode.IsChecked)
                                 PlaySound(Sounds[ Rnd.Next(0,Sounds.Count-1)]);
                            else
                                await speech.Read($"I saw {PersonCount} person in {DataConfig.RoomName[CCTVIndex]}");
                        }
                    }

                    // Update the displayed performance text
                    StatusLbl.Text = $"bind: {m_bindTime.ToString("F2")}ms, eval: {m_evalTime.ToString("F2")}ms";
                }
                catch (TaskCanceledException)
                {
                    // no-op: we expect this exception when we change media sources
                    // and can safely ignore/continue
                }
                catch (Exception ex)
                {
                    NotifyUser($"Exception while rendering results: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Triggered when the image control is resized, making sure the canvas size stays in sync with the frame display control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIProcessedPreview_SizeChanged1(object sender, SizeChangedEventArgs e)
        {
            // Make sure the aspect ratio is honored when rendering the body limbs
            //float cameraAspectRatio = (float)m_frameSource.FrameWidth / m_frameSource.FrameHeight;
            float previewAspectRatio = (float)(CCTV1.ActualWidth / CCTV1.ActualHeight);
            var cameraAspectRatio = previewAspectRatio;
            UIOverlayCanvas1.Width = cameraAspectRatio >= previewAspectRatio ? CCTV1.ActualWidth : CCTV1.ActualHeight * cameraAspectRatio;
            UIOverlayCanvas1.Height = cameraAspectRatio >= previewAspectRatio ? CCTV1.ActualWidth / cameraAspectRatio : CCTV1.ActualHeight;

            m_bboxRenderer[0].ResizeContent(e);
        }
        private void UIProcessedPreview_SizeChanged2(object sender, SizeChangedEventArgs e)
        {
            // Make sure the aspect ratio is honored when rendering the body limbs
            //float cameraAspectRatio = (float)m_frameSource.FrameWidth / m_frameSource.FrameHeight;
            float previewAspectRatio = (float)(CCTV2.ActualWidth / CCTV2.ActualHeight);
            var cameraAspectRatio = previewAspectRatio;
            UIOverlayCanvas2.Width = cameraAspectRatio >= previewAspectRatio ? CCTV2.ActualWidth : CCTV2.ActualHeight * cameraAspectRatio;
            UIOverlayCanvas2.Height = cameraAspectRatio >= previewAspectRatio ? CCTV2.ActualWidth / cameraAspectRatio : CCTV2.ActualHeight;

            m_bboxRenderer[1].ResizeContent(e);
        }
        private void UIProcessedPreview_SizeChanged3(object sender, SizeChangedEventArgs e)
        {
            // Make sure the aspect ratio is honored when rendering the body limbs
            //float cameraAspectRatio = (float)m_frameSource.FrameWidth / m_frameSource.FrameHeight;
            float previewAspectRatio = (float)(CCTV3.ActualWidth / CCTV3.ActualHeight);
            var cameraAspectRatio = previewAspectRatio;
            UIOverlayCanvas3.Width = cameraAspectRatio >= previewAspectRatio ? CCTV3.ActualWidth : CCTV3.ActualHeight * cameraAspectRatio;
            UIOverlayCanvas3.Height = cameraAspectRatio >= previewAspectRatio ? CCTV3.ActualWidth / cameraAspectRatio : CCTV3.ActualHeight;

            m_bboxRenderer[2].ResizeContent(e);
        }
        private void UIProcessedPreview_SizeChanged4(object sender, SizeChangedEventArgs e)
        {
            // Make sure the aspect ratio is honored when rendering the body limbs
            //float cameraAspectRatio = (float)m_frameSource.FrameWidth / m_frameSource.FrameHeight;
            float previewAspectRatio = (float)(CCTV4.ActualWidth / CCTV4.ActualHeight);
            var cameraAspectRatio = previewAspectRatio;
            UIOverlayCanvas4.Width = cameraAspectRatio >= previewAspectRatio ? CCTV4.ActualWidth : CCTV4.ActualHeight * cameraAspectRatio;
            UIOverlayCanvas4.Height = cameraAspectRatio >= previewAspectRatio ? CCTV4.ActualWidth / cameraAspectRatio : CCTV4.ActualHeight;

            m_bboxRenderer[3].ResizeContent(e);
        }
    }
}
