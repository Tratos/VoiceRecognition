using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Speech.Recognition;
using System.Speech.Synthesis;


namespace VoiceRecognition
{

    public partial class MainView : Form
    {

        private System.Speech.Recognition.SpeechRecognitionEngine engine = new SpeechRecognitionEngine();
        private SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        private List<Image> imageList = new List<Image>();
        private Image baseImage = Image.FromFile(
            Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath),
                "../../assets/test.png"
            )
        );

        public MainView()
        {
            synthesizer.Speak("Booting the application.");
            InitializeComponent();
            imageList.Add(baseImage);
        }

        private void MainView_Load(object sender, EventArgs e)
        {

            engine.SetInputToDefaultAudioDevice();
            engine.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold", 70);
            engine.UnloadAllGrammars();

            Grammar brightnessGrammar = BrightnessGrammar();
            brightnessGrammar.Enabled = true;
            engine.LoadGrammar(brightnessGrammar);

            Grammar contrastGrammar = ContrastGrammar();
            contrastGrammar.Enabled = true;
            engine.LoadGrammar(contrastGrammar);

            Grammar grayscaleGrammar = GrayscaleGrammar();
            grayscaleGrammar.Enabled = true;
            engine.LoadGrammar(grayscaleGrammar);

            Grammar invertGrammar = InvertGrammar();
            invertGrammar.Enabled = true;
            engine.LoadGrammar(invertGrammar);

            Grammar colorFilterGrammar = ColorFilterGrammar();
            colorFilterGrammar.Enabled = true;
            engine.LoadGrammar(colorFilterGrammar);

            Grammar flipGrammar = FlipGrammar();
            flipGrammar.Enabled = true;
            engine.LoadGrammar(flipGrammar);

            Grammar rotateGrammar = RotateGrammar();
            rotateGrammar.Enabled = true;
            engine.LoadGrammar(rotateGrammar);

            Grammar undoGrammar = UndoGrammar();
            undoGrammar.Enabled = true;
            engine.LoadGrammar(undoGrammar);

            engine.RecognizeAsync(RecognizeMode.Multiple);
            engine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(engineSpeechRecognizer);

            synthesizer.Speak("Application ready.");
        }

        void engineSpeechRecognizer(object sender, SpeechRecognizedEventArgs e)
        {
            SemanticValue semantics = e.Result.Semantics;
            //string rawText = e.Result.Text;
            //RecognitionResult result = e.Result();

            if (semantics.ContainsKey("brightness"))
            {
                Image tmpImage = SetBrightness((int)semantics["brightness"].Value);
                imageList.Add(tmpImage);
            }
            else if (semantics.ContainsKey("contrast"))
            {
                Image tmpImage = SetContrast((double)semantics["contrast"].Value);
                imageList.Add(tmpImage);
            }
            else if (semantics.ContainsKey("grayscale"))
            {
                Image tmpImage = SetGrayscale();
                imageList.Add(tmpImage);
            }
            else if (semantics.ContainsKey("invert"))
            {
                Image tmpImage = SetInvert();
                imageList.Add(tmpImage);
            }
            else if (semantics.ContainsKey("colorFilter"))
            {
                Color color = Color.FromArgb((int)semantics["colorFilter"].Value);
                Image tmpImage = SetColorFilter(color);
                imageList.Add(tmpImage);
            }
            else if (semantics.ContainsKey("flip"))
            {
                Image lastImage = LastImage();
                Image tmpImage = (Image)lastImage.Clone();
                tmpImage.RotateFlip(RotateFlipType.Rotate180FlipY);
                imageList.Add(tmpImage);
            }
            else if (semantics.ContainsKey("rotate"))
            {
                Image lastImage = LastImage();
                Image tmpImage = (Image)lastImage.Clone();
                tmpImage.RotateFlip(RotateFlipType.Rotate90FlipY);
                imageList.Add(tmpImage);
            }
            else if (semantics.ContainsKey("undo"))
            {
                if (imageList.Count > 1)
                {
                    imageList.RemoveAt(imageList.Count - 1);
                }
                else {
                    synthesizer.Speak("There is no more redo actions.");
                }
            }

            RenderImage();
        }

        void RenderImage()
        {
            Image tmpImage = LastImage();
            this.pictureBox.Image = tmpImage;
        }

        Image LastImage()
        {
            return imageList[imageList.Count - 1];
        }

        /* CONVERT METHODS */
        Image SetBrightness(int brightness)
        {
            Bitmap bmap = new Bitmap(LastImage());
            if (brightness < -255) brightness = -255;
            if (brightness > 255) brightness = 255;
            Color c;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    c = bmap.GetPixel(i, j);
                    int cR = c.R + brightness;
                    int cG = c.G + brightness;
                    int cB = c.B + brightness;

                    if (cR < 0) cR = 1;
                    if (cR > 255) cR = 255;

                    if (cG < 0) cG = 1;
                    if (cG > 255) cG = 255;

                    if (cB < 0) cB = 1;
                    if (cB > 255) cB = 255;

                    bmap.SetPixel(i, j,
        Color.FromArgb((byte)cR, (byte)cG, (byte)cB));
                }
            }
            return (Image)bmap;
        }

        Image SetContrast(double contrast)
        {
            Bitmap bmap = new Bitmap(LastImage());
            if (contrast < -100) contrast = -100;
            if (contrast > 100) contrast = 100;
            contrast = (100.0 + contrast) / 100.0;
            contrast *= contrast;
            Color c;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    c = bmap.GetPixel(i, j);
                    double pR = c.R / 255.0;
                    pR -= 0.5;
                    pR *= contrast;
                    pR += 0.5;
                    pR *= 255;
                    if (pR < 0) pR = 0;
                    if (pR > 255) pR = 255;

                    double pG = c.G / 255.0;
                    pG -= 0.5;
                    pG *= contrast;
                    pG += 0.5;
                    pG *= 255;
                    if (pG < 0) pG = 0;
                    if (pG > 255) pG = 255;

                    double pB = c.B / 255.0;
                    pB -= 0.5;
                    pB *= contrast;
                    pB += 0.5;
                    pB *= 255;
                    if (pB < 0) pB = 0;
                    if (pB > 255) pB = 255;

                    bmap.SetPixel(i, j, Color.FromArgb((byte)pR, (byte)pG, (byte)pB));
                }
            }
            return (Image)bmap;
        }

        Image SetGrayscale()
        {
            Bitmap bmap = new Bitmap(LastImage());
            Color c;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    c = bmap.GetPixel(i, j);
                    byte gray = (byte)(.299 * c.R + .587 * c.G + .114 * c.B);

                    bmap.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                }
            }
            return (Image)bmap;
        }

        Image SetInvert()
        {
            Bitmap bmap = new Bitmap(LastImage());
            Color c;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    c = bmap.GetPixel(i, j);
                    bmap.SetPixel(i, j, Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B));
                }
            }
            return (Image)bmap;
        }

        Image SetColorFilter(Color color)
        {
            Bitmap bmap = new Bitmap(LastImage());
            Color c;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    c = bmap.GetPixel(i, j);
                    int nPixelR = 0;
                    int nPixelG = 0;
                    int nPixelB = 0;

                    nPixelR = c.R - (255 - color.R);
                    nPixelG = c.G - (255 - color.G);
                    nPixelB = c.B - (255 - color.B);
                    //if (color == Color.Red)
                    //{
                    //    nPixelR = c.R;
                    //    nPixelG = c.G - 255;
                    //    nPixelB = c.B - 255;
                    //}
                    //else if (color == Color.Green)
                    //{
                    //    nPixelR = c.R - 255;
                    //    nPixelG = c.G;
                    //    nPixelB = c.B - 255;
                    //}
                    //else if (color == Color.Blue)
                    //{
                    //    nPixelR = c.R - 255;
                    //    nPixelG = c.G - 255;
                    //    nPixelB = c.B;
                    //}
                    nPixelR = Math.Max(nPixelR, 0);
                    nPixelR = Math.Min(255, nPixelR);

                    nPixelG = Math.Max(nPixelG, 0);
                    nPixelG = Math.Min(255, nPixelG);

                    nPixelB = Math.Max(nPixelB, 0);
                    nPixelB = Math.Min(255, nPixelB);

                    bmap.SetPixel(i, j, Color.FromArgb((byte)nPixelR,
                    (byte)nPixelG, (byte)nPixelB));
                }
            }
            return (Image)bmap;
        }

        /* GRAMMARS */
        private Grammar ContrastGrammar()
        {
            // Change/Set Contrast to Choices
            var choices = new Choices();
            for (var i = -100; i <= 100; i++)
            {
                choices.Add(i.ToString());
            }

            GrammarBuilder changeGrammar = "Change";
            GrammarBuilder setGrammar = "Set";
            GrammarBuilder contrastGrammar = "Contrast";
            GrammarBuilder toGrammar = "To";
            var choicesGramar = new GrammarBuilder(choices);

            SemanticResultKey resultKey = new SemanticResultKey("contrast", choices);
            GrammarBuilder resultContrast = new GrammarBuilder(resultKey);

            Choices alternatives = new Choices(changeGrammar, setGrammar);

            GrammarBuilder result = new GrammarBuilder(alternatives);
            result.Append(contrastGrammar);
            result.Append(toGrammar);
            result.Append(resultContrast);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Set Contrast";
            return grammar;
        }

        private Grammar BrightnessGrammar()
        {
            // Change/Set Brightness to Choices
            var choices = new Choices();
            for (var i = -255; i <= 255; i++)
            {
                choices.Add(i.ToString());
            }

            GrammarBuilder changeGrammar = "Change";
            GrammarBuilder setGrammar = "Set";
            GrammarBuilder brightnessGrammar = "Brightness";
            GrammarBuilder toGrammar = "To";
            var choicesGramar = new GrammarBuilder(choices);

            SemanticResultKey resultKey = new SemanticResultKey("brightness", choices);
            GrammarBuilder resultContrast = new GrammarBuilder(resultKey);

            Choices alternatives = new Choices(changeGrammar, setGrammar);

            GrammarBuilder result = new GrammarBuilder(alternatives);
            result.Append(brightnessGrammar);
            result.Append(toGrammar);
            result.Append(resultContrast);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Set Contrast";
            return grammar;
        }

        private Grammar GrayscaleGrammar()
        {
            // Change/Convert to grayscale

            GrammarBuilder changeGrammar = "Change";
            GrammarBuilder convertGrammar = "Convert";
            GrammarBuilder toGrammar = "To";
            GrammarBuilder grayscaleGrammar = "Grayscale";

            Choices alternatives = new Choices(changeGrammar, convertGrammar);
            Choices commands = new Choices(grayscaleGrammar);

            SemanticResultKey resultKey = new SemanticResultKey("grayscale", commands);

            GrammarBuilder resultGrayscale = new GrammarBuilder(resultKey);

            GrammarBuilder result = new GrammarBuilder(alternatives);
            result.Append(toGrammar);
            result.Append(resultGrayscale);
            Grammar grammar = new Grammar(result);
            grammar.Name = "Convert to Grayscale";
            return grammar;
        }

        private Grammar InvertGrammar()
        {
            // Invert Image
            GrammarBuilder invert = "Invert";

            Choices commands = new Choices(invert);

            SemanticResultKey resultKey = new SemanticResultKey("invert", commands);

            GrammarBuilder image = "Image";

            Choices text = new Choices(image);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(text);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Invert Image";
            return grammar;
        }

        private Grammar ColorFilterGrammar()
        {
            // Add/Set Filter to Choices

            GrammarBuilder addGrammar = "Add";
            GrammarBuilder setGrammar = "Set";
            GrammarBuilder filterGrammar = "Filter";
            GrammarBuilder toGrammar = "To";

            Choices colorChoice = new Choices();

            foreach (string colorName in System.Enum.GetNames(typeof(KnownColor)))
            {
                SemanticResultValue choiceResultValue = new SemanticResultValue(colorName, Color.FromName(colorName).ToArgb());
                GrammarBuilder resultValueBuilder = new GrammarBuilder(choiceResultValue);
                colorChoice.Add(resultValueBuilder);
            }

            SemanticResultKey resultKey = new SemanticResultKey("colorFilter", colorChoice);
            GrammarBuilder resultContrast = new GrammarBuilder(resultKey);

            Choices alternatives = new Choices(addGrammar, setGrammar);

            GrammarBuilder result = new GrammarBuilder(alternatives);
            result.Append(filterGrammar);
            result.Append(toGrammar);
            result.Append(resultContrast);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Set Contrast";
            return grammar;
        }

        private Grammar FlipGrammar()
        {
            // Flip Image
            GrammarBuilder flip = "Flip";

            Choices commands = new Choices(flip);

            SemanticResultKey resultKey = new SemanticResultKey("flip", commands);

            GrammarBuilder image = "Image";

            Choices text = new Choices(image);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(text);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Flip Image";
            return grammar;
        }

        private Grammar RotateGrammar()
        {
            // Rotate Image
            GrammarBuilder rotate = "Rotate";

            Choices commands = new Choices(rotate);

            SemanticResultKey resultKey = new SemanticResultKey("rotate", commands);

            GrammarBuilder image = "Image";

            Choices text = new Choices(image);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(text);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Rotate Image";
            return grammar;
        }

        private Grammar UndoGrammar()
        {
            // Undo
            GrammarBuilder undo = "Undo";

            Choices commands = new Choices(undo);

            SemanticResultKey resultKey = new SemanticResultKey("undo", commands);

            GrammarBuilder result = new GrammarBuilder(resultKey);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Undo Action";
            return grammar;
        }

    }
}
