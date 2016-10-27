﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ObjLoader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DrawManager _drawMan;

        public MainPage()
        {
            this.InitializeComponent();

            _drawMan = new DrawManager(panel, new Triangle.Triangle());
            //            _drawMan = new DrawManager(panel, new GreenRect());
        }

        private void Panel_OnLoaded(object sender, RoutedEventArgs e)
        {
            _drawMan.Init();
        }

        private void Panel_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _drawMan.Deinit();
        }
    }
}