﻿using System;
using System.Drawing;
using System.IO;
using System.Windows;
using BFML._3D;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System.Windows.Input;
using BFML.Core;
using CommonData.Models;
using TCPFileClient;

namespace BFML.WPF;

public partial class MainWindow : Window
{
    private readonly FileClient _fileClient;
    private readonly User _user;
    private readonly LocalPrefs _localPrefs;
    private readonly ConfigurationVersion _configVersion;
    private readonly LaunchConfiguration _launchConfig;
    private SkinPreviewRenderer _skinPreviewRenderer;

    public MainWindow(FileClient fileClient, User user, LocalPrefs localPrefs, 
       LaunchConfiguration launchConfig, ConfigurationVersion configVersion)
    {
        InitializeComponent();
        _fileClient = fileClient;
        _user = user;
        _localPrefs = localPrefs;
        //TODO should correspond to skin.png in .minecraft/BFML/skin.png
        _launchConfig = launchConfig;
        _configVersion = configVersion;

        SetUpSkinRenderer();
        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs args)
    {
        CheckIfUserPaid();
        Loaded -= OnWindowLoaded;
    }

    private void CheckIfUserPaid()
    {
        if (_user.GryvnyasPaid < _launchConfig.RequiredGriwnas)
        {
            //DisablePlayButton();
        }
    }

    private void DisablePlayButton()
    {
        throw new NotImplementedException();
    }
    
    #region PlayerModelRendering

    private void SetUpSkinRenderer()
    {
        GLWpfControlSettings settings = new GLWpfControlSettings
        {
            MajorVersion = 4,
            MinorVersion = 0
        };
        OpenTkControl.Start(settings);
        _skinPreviewRenderer = new SkinPreviewRenderer();
        _skinPreviewRenderer.SetUp();
    }
    
    private void SkinPreviewOnRender(TimeSpan obj)
    {
        _skinPreviewRenderer.Render();
    }

    #endregion

    #region Navigation

    private void MoveWindow(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (WindowState == WindowState.Maximized) 
            {
                WindowState = WindowState.Normal;
                Application.Current.MainWindow.Top = 3;
            }
            DragMove();
        }
    }

    private void ShutDown(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
    private void Minimize(object sender, RoutedEventArgs e)
    {
        try
        {
            this.WindowState = WindowState.Minimized;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    #endregion

    private async void OnSkinFileDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        string pngDirectory = Path.GetDirectoryName(files[0]);
        string pngName = Path.GetFileName(files[0]);

        await _fileClient.SendSkinChangeRequest(_user.Nickname, pngDirectory, pngName);
    }

    private void OnPlayButton(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}