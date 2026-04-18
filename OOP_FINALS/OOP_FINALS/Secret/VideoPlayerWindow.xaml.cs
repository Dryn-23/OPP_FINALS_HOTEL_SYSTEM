using System;
using System.Windows;
using System.Windows.Media;

namespace OOP_FINALS.Secret
{
    public partial class VideoPlayerWindow : Window
    {
        public VideoPlayerWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
         
            LoadAndPlayVideo();
        }

        private void LoadAndPlayVideo()
        {
            try
            {
              
                string managerVideoPath = @"C:\Users\GARIN\Downloads\AQPSExlNyrxJXuUtVIoUuvSOUjKc2zLtMQR0e9a0EcWzKVj_G_38XndPl9RiqYL7YV1l9U5z8gTGvuFJgYBXiyCkZr4GD4VuYY5smPCwPw.mp4";

                MainVideoPlayer.Source = new Uri(managerVideoPath, UriKind.Absolute);
                MainVideoPlayer.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Video load error: {ex.Message}\nUsing fallback video...");
             
                MainVideoPlayer.Source = new Uri("pack://application:,,,/fallback-video.mp4");
                MainVideoPlayer.Play();
            }
        }

        private void MainVideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
           
            MainVideoPlayer.Position = TimeSpan.Zero;
            MainVideoPlayer.Play();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            MainVideoPlayer.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            MainVideoPlayer.Pause();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            MainVideoPlayer.Stop();
            MainVideoPlayer.Position = TimeSpan.Zero;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Clean up video when closing
            MainVideoPlayer.Stop();
            MainVideoPlayer.Source = null;
        }
    }
}