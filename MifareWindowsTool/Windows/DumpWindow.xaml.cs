﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace MCT_Windows.Windows
{
    /// <summary>
    /// Logique d'interaction pour DumpWindow.xaml
    /// </summary>
    public partial class DumpWindow : Window
    {
        public List<string> LinesA { get; set; } = new List<string>();
        public List<string> LinesB { get; set; } = new List<string>();
        byte[] bytesDataA = null;
        byte[] bytesDataB = null;
        bool bConvertoAscii = true;
        Tools Tools { get; set; }
        int split = 8;
        OpenFileDialog ofd = new OpenFileDialog();
        public DumpWindow(Tools t, string fileName, bool bCompareDumpsMode = false)
        {
            InitializeComponent();
            ofd.Filter = "Dump Files|*.dump|All Files|*.*";
            ofd.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Tools = t;
            if (bCompareDumpsMode)
            {
                Title = "Compare Dumps";
                btnSaveDump.Visibility = Visibility.Hidden;
                stkOpenDumps.Visibility = Visibility.Visible;
            }
            else
            {
                btnSaveDump.Visibility = Visibility.Visible;
                stkOpenDumps.Visibility = Visibility.Collapsed;
                bytesDataA = File.ReadAllBytes(fileName);
                if (bytesDataA.Length == 1024) split = 4;
                ShowHex();
            }

        }

        private void ShowHex()
        {
            txtOutput.Document = new System.Windows.Documents.FlowDocument();
            string hex = BitConverter.ToString(bytesDataA).Replace("-", string.Empty);

            LinesA = Split(hex, 32);
            int sector = (LinesA.Count - split) / split;
            for (int i = LinesA.Count - split; i >= 0; i -= split)
                LinesA.Insert(i, $"+Sector: {sector--}\n");

            txtOutput.AppendText(new string(LinesA.SelectMany(c => c).ToArray()));
        }

        static List<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize) + "\n").ToList();
        }

        private void btnSaveDump_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Dump Files|*.dump";
            var dr = sfd.ShowDialog();
            if (dr.Value)
                File.WriteAllBytes(sfd.FileName, bytesDataA);

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnShowAsAscii_Click(object sender, RoutedEventArgs e)
        {
            if (bConvertoAscii)
            {
                btnShowAsAscii.Content = "Show as Hex";
                ShowAscii();
            }
            else
            {
                btnShowAsAscii.Content = "Show as ASCII";
                ShowHex();
            }
            bConvertoAscii = !bConvertoAscii;
        }

        private void ShowAscii()
        {
            string hex = BitConverter.ToString(bytesDataA).Replace("-", string.Empty);
            var ascii = Tools.ConvertHex(hex);
            LinesA = Split(ascii, 32);
            int sector = (LinesA.Count - 4) / 4;
            for (int i = LinesA.Count - 4; i >= 0; i -= 4)
                LinesA.Insert(i, $"+Sector: {sector--}\n");
            txtOutput.Document = new System.Windows.Documents.FlowDocument();
            txtOutput.AppendText(new string(LinesA.SelectMany(c => c).ToArray()));
        }
        private void BtnOpenDumpA_Click(object sender, RoutedEventArgs e)
        {

            var dr = ofd.ShowDialog();
            if (dr.Value)
            {
                btnOpenDumpA.Content = $"Open Dump A: {Path.GetFileName(ofd.FileName)}";
                bytesDataA = File.ReadAllBytes(ofd.FileName);
                ShowCompareDumps();
            }

        }

        private void BtnOpenDumpB_Click(object sender, RoutedEventArgs e)
        {
            var dr = ofd.ShowDialog();
            if (dr.Value)
            {
                btnOpenDumpB.Content = $"Open Dump B: {Path.GetFileName(ofd.FileName)}";
                bytesDataB = File.ReadAllBytes(ofd.FileName);
                ShowCompareDumps();
            }
        }

        private void ShowCompareDumps()
        {
            if (bytesDataA == null || bytesDataB == null) return;

            string hexA = BitConverter.ToString(bytesDataA).Replace("-", string.Empty);
            string hexB = BitConverter.ToString(bytesDataB).Replace("-", string.Empty);

            LinesA = Split(hexA, 32);
            LinesB = Split(hexB, 32);
            if (bytesDataA.Length == 1024) split = 4;


            var mixedLines = new List<string>();
            int sectorA = (LinesA.Count - split) / split;
            for (int i = LinesA.Count - split; i >= 0; i -= split)
                LinesA.Insert(i, $"+Sector: {sectorA--}\n");

            int sectorB = (LinesB.Count - split) / split;
            for (int i = LinesB.Count - split; i >= 0; i -= split)
                LinesB.Insert(i, "");

            for (int i = 0; i < Math.Max(LinesA.Count, LinesB.Count); i++)
            {
                if (i < LinesA.Count && !LinesA[i].StartsWith("+") || i < LinesB.Count && !LinesB[i].StartsWith("+"))
                {
                    if (i < LinesA.Count && i < LinesB.Count &&  LinesA[i] == LinesB[i])
                    {
                        mixedLines.Add("___________Identical____________");
                    }
                    else if (i < LinesB.Count  && !string.IsNullOrWhiteSpace(LinesB[i]))
                    {
                        mixedLines.Add("___________Different____________");
                        var diffs = "  ";
                        for (int j = 0; j < LinesA[i].Count(); j++)
                        {
                            diffs += (LinesA[i][j] != LinesB[i][j]) ? "v" : " ";
                        }
                        mixedLines.Add(diffs+"\n");
                    }
                }
                if (i < LinesA.Count)
                    mixedLines.Add((!string.IsNullOrWhiteSpace(LinesA[i]) && !LinesA[i].StartsWith("+") ? "A:" : "") + LinesA[i]);
                if (i < LinesB.Count)
                    mixedLines.Add((!string.IsNullOrWhiteSpace(LinesB[i]) && !LinesB[i].StartsWith("+") ? "B:" : "") + LinesB[i]);
            }

            txtOutput.AppendText(new string(mixedLines.SelectMany(c => c).ToArray()));



        }


    }
}