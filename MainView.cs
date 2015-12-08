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

        // Crop Logic
        private Boolean isCroping = false;
        private int cropX = 0;
        private int cropY = 0;
        private int cropWidth = 0;
        private int cropHeight = 0;

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

            LoadAllGrammars();

            engine.RecognizeAsync(RecognizeMode.Multiple);
            engine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(engineSpeechRecognizer);

            synthesizer.Speak("Application ready.");
        }

        private void LoadAllGrammars()
        {
            engine.UnloadAllGrammars();

            Grammar initCropGrammar = InitCropGrammar();
            initCropGrammar.Enabled = true;
            engine.LoadGrammar(initCropGrammar);

            Grammar cropGrammar = CropGrammar();
            cropGrammar.Enabled = true;
            engine.LoadGrammar(cropGrammar);

            Grammar cropPositionGrammar = CropPositionGrammar();
            cropPositionGrammar.Enabled = true;
            engine.LoadGrammar(cropPositionGrammar);

            Grammar cropWidthAndHeigthGrammar = CropWidthAndHeigthGrammar();
            cropWidthAndHeigthGrammar.Enabled = true;
            engine.LoadGrammar(cropWidthAndHeigthGrammar);

            Grammar cropCancelGrammar = CropCancelGrammar();
            cropCancelGrammar.Enabled = true;
            engine.LoadGrammar(cropCancelGrammar);

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
        }

        void engineSpeechRecognizer(object sender, SpeechRecognizedEventArgs e)
        {
            SemanticValue semantics = e.Result.Semantics;
            string rawText = e.Result.Text;

            if (semantics.ContainsKey("undo"))
            {
                isCroping = false;
                if (imageList.Count > 1)
                {
                    RemoveImage();
                    this.statusLabel.Text = rawText;
                }
                else
                {
                    var text = "There are no more undo actions.";
                    this.statusLabel.Text = text;
                    synthesizer.Speak(text);
                }

            }
            else if (!isCroping)
            {
                if (semantics.ContainsKey("init_crop"))
                {
                    isCroping = true;
                    LoadAllGrammars();
                    cropX = LastImage().Width / 4;
                    cropY = LastImage().Height / 4;
                    cropWidth = LastImage().Width / 2;
                    cropHeight = LastImage().Height / 2;
                    Image tmpImage = DrawOutCropArea(cropX, cropY, cropWidth, cropHeight);
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("brightness"))
                {
                    Image tmpImage = SetBrightness((int)semantics["brightness"].Value);
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("contrast"))
                {
                    Image tmpImage = SetContrast(Convert.ToDouble(semantics["contrast"].Value));
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("grayscale"))
                {
                    Image tmpImage = SetGrayscale();
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("invert"))
                {
                    Image tmpImage = SetInvert();
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("colorFilter"))
                {
                    Color color = Color.FromArgb((int)semantics["colorFilter"].Value);
                    Image tmpImage = SetColorFilter(color);
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("flip"))
                {
                    Image lastImage = LastImage();
                    Image tmpImage = (Image)lastImage.Clone();
                    tmpImage.RotateFlip(RotateFlipType.Rotate180FlipY);
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("rotate"))
                {
                    Image lastImage = LastImage();
                    Image tmpImage = (Image)lastImage.Clone();
                    tmpImage.RotateFlip(RotateFlipType.Rotate90FlipY);
                    SetImageAndText(tmpImage, rawText);
                }
            }
            else
            {
                if (semantics.ContainsKey("crop"))
                {
                    Image tmpImage = Crop(cropX, cropY, cropWidth, cropHeight);
                    RemoveImage();
                    SetImageAndText(tmpImage, rawText);
                    isCroping = false;
                }
                else if (semantics.ContainsKey("crop_position_x") || semantics.ContainsKey("crop_position_y"))
                {
                    cropX = (int)semantics["crop_position_x"].Value;
                    cropY = (int)semantics["crop_position_y"].Value;
                    RemoveImage();
                    Image tmpImage = DrawOutCropArea(cropX, cropY, cropWidth, cropHeight);
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("crop_width") || semantics.ContainsKey("crop_height"))
                {
                    cropWidth = (int)semantics["crop_width"].Value;
                    cropHeight = (int)semantics["crop_height"].Value;
                    RemoveImage();
                    Image tmpImage = DrawOutCropArea(cropX, cropY, cropWidth, cropHeight);
                    SetImageAndText(tmpImage, rawText);
                }
                else if (semantics.ContainsKey("cancel_crop"))
                {
                    RemoveImage();
                    this.statusLabel.Text = rawText;
                    isCroping = false;
                }
            }

            RenderImage();
        }

        // HELPERS
        void RenderImage()
        {
            Image tmpImage = LastImage();
            this.pictureBox.Image = tmpImage;
        }

        void SetImageAndText(Image image, String text)
        {
            imageList.Add(image);
            this.statusLabel.Text = text;
        }

        void RemoveImage()
        {
            imageList.RemoveAt(imageList.Count - 1);
        }

        Image LastImage()
        {
            return imageList[imageList.Count - 1];
        }

        /* CONVERT METHODS */
        Image DrawOutCropArea(int xPosition, int yPosition, int width, int height)
        {
            Bitmap bmap = new Bitmap(LastImage());
            Graphics gr = Graphics.FromImage(bmap);
            Brush cBrush = new Pen(Color.FromArgb(150, Color.White)).Brush;
            Rectangle rect1 = new Rectangle(0, 0, LastImage().Width, yPosition);
            Rectangle rect2 = new Rectangle(0, yPosition, xPosition, height);
            Rectangle rect3 = new Rectangle
            (0, (yPosition + height), LastImage().Width, LastImage().Height);
            Rectangle rect4 = new Rectangle((xPosition + width), yPosition, (LastImage().Width - xPosition - width), height);
            gr.FillRectangle(cBrush, rect1);
            gr.FillRectangle(cBrush, rect2);
            gr.FillRectangle(cBrush, rect3);
            gr.FillRectangle(cBrush, rect4);
            return (Image)bmap;
        }

        Image Crop(int xPosition, int yPosition, int width, int height)
        {
            Bitmap bmap = new Bitmap(LastImage());
            if (xPosition + width > LastImage().Width)
                width = LastImage().Width - xPosition;
            if (yPosition + height > LastImage().Height)
                height = LastImage().Height - yPosition;
            Rectangle rect = new Rectangle(xPosition, yPosition, width, height);
            return (Image)bmap.Clone(rect, bmap.PixelFormat);
        }

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

                    bmap.SetPixel(i, j, Color.FromArgb((byte)cR, (byte)cG, (byte)cB));
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
        private Grammar InitCropGrammar()
        {
            // Init Crop
            GrammarBuilder init = "Init";
            GrammarBuilder crop = "Crop";

            Choices commands = new Choices(init);

            SemanticResultKey resultKey = new SemanticResultKey("init_crop", commands);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(crop);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Init Crop";
            return grammar;
        }

        private Grammar CropGrammar()
        {
            // Crop Image
            GrammarBuilder crop = "Crop";
            GrammarBuilder image = "Image";

            Choices commands = new Choices(crop);

            SemanticResultKey resultKey = new SemanticResultKey("crop", commands);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(image);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Crop Image";
            return grammar;
        }

        private Grammar CropCancelGrammar()
        {
            // Cancel Crop
            GrammarBuilder cancel = "Cancel";
            GrammarBuilder crop = "Crop";

            Choices commands = new Choices(cancel);

            SemanticResultKey resultKey = new SemanticResultKey("cancel_crop", commands);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(crop);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Cancel Crop";
            return grammar;
        }

        private Grammar CropPositionGrammar()
        {
            // Change/Set Crop Position to X and Y
            var choicesX = new Choices();
            var choicesY = new Choices();

            for (var i = 0; i <= LastImage().Width; i++)
            {
                SemanticResultValue choiceResultValue = new SemanticResultValue(i.ToString(), i);
                GrammarBuilder resultValueBuilder = new GrammarBuilder(choiceResultValue);
                choicesX.Add(resultValueBuilder);
            }

            for (var i = 0; i <= LastImage().Height; i++)
            {
                SemanticResultValue choiceResultValue = new SemanticResultValue(i.ToString(), i);
                GrammarBuilder resultValueBuilder = new GrammarBuilder(choiceResultValue);
                choicesY.Add(resultValueBuilder);
            }

            GrammarBuilder changeGrammar = "Change";
            GrammarBuilder setGrammar = "Set";
            GrammarBuilder cropGrammar = "Crop";
            GrammarBuilder positionGrammar = "Position";
            GrammarBuilder toGrammar = "To";
            GrammarBuilder andGrammar = "And";

            SemanticResultKey resultKeyX = new SemanticResultKey("crop_position_x", choicesX);
            GrammarBuilder resultCropX = new GrammarBuilder(resultKeyX);

            SemanticResultKey resultKeyY = new SemanticResultKey("crop_position_y", choicesY);
            GrammarBuilder resultCropY = new GrammarBuilder(resultKeyY);

            Choices alternatives = new Choices(changeGrammar, setGrammar);

            GrammarBuilder result = new GrammarBuilder(alternatives);
            result.Append(cropGrammar);
            result.Append(positionGrammar);
            result.Append(toGrammar);
            result.Append(resultCropX);
            result.Append(andGrammar);
            result.Append(resultCropY);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Set Crop Position";
            return grammar;
        }

        private Grammar CropWidthAndHeigthGrammar()
        {
            // Change/Set Crop Width to X and Height to Y
            var choicesWidth = new Choices();
            var choicesHeight = new Choices();

            Console.WriteLine(LastImage().Width);
            Console.WriteLine(LastImage().Height);

            for (var i = 0; i <= LastImage().Width; i++)
            {
                SemanticResultValue choiceResultValue = new SemanticResultValue(i.ToString(), i);
                GrammarBuilder resultValueBuilder = new GrammarBuilder(choiceResultValue);
                choicesWidth.Add(resultValueBuilder);
            }

            for (var i = 0; i <= LastImage().Height; i++)
            {
                SemanticResultValue choiceResultValue = new SemanticResultValue(i.ToString(), i);
                GrammarBuilder resultValueBuilder = new GrammarBuilder(choiceResultValue);
                choicesHeight.Add(resultValueBuilder);
            }

            GrammarBuilder changeGrammar = "Change";
            GrammarBuilder setGrammar = "Set";
            GrammarBuilder cropGrammar = "Crop";
            GrammarBuilder widthGrammar = "Width";
            GrammarBuilder heightGrammar = "Height";
            GrammarBuilder toGrammar = "To";
            GrammarBuilder andGrammar = "And";

            SemanticResultKey resultKeyWidth = new SemanticResultKey("crop_width", choicesWidth);
            GrammarBuilder resultCropWidth = new GrammarBuilder(resultKeyWidth);

            SemanticResultKey resultKeyHeight = new SemanticResultKey("crop_height", choicesHeight);
            GrammarBuilder resultCropHeight = new GrammarBuilder(resultKeyHeight);

            Choices alternatives = new Choices(changeGrammar, setGrammar);

            GrammarBuilder result = new GrammarBuilder(alternatives);
            result.Append(cropGrammar);
            result.Append(widthGrammar);
            result.Append(toGrammar);
            result.Append(resultCropWidth);
            result.Append(andGrammar);
            result.Append(heightGrammar);
            result.Append(toGrammar);
            result.Append(resultCropHeight);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Set Crop Width And Height";
            return grammar;
        }

        private Grammar BrightnessGrammar()
        {
            // Change/Set Brightness to Choices
            var choices = new Choices();
            for (var i = -255; i <= 255; i++)
            {
                SemanticResultValue choiceResultValue = new SemanticResultValue(i.ToString(), i);
                GrammarBuilder resultValueBuilder = new GrammarBuilder(choiceResultValue);
                choices.Add(resultValueBuilder);
            }

            GrammarBuilder changeGrammar = "Change";
            GrammarBuilder setGrammar = "Set";
            GrammarBuilder brightnessGrammar = "Brightness";
            GrammarBuilder toGrammar = "To";

            SemanticResultKey resultKey = new SemanticResultKey("brightness", choices);
            GrammarBuilder resultContrast = new GrammarBuilder(resultKey);

            Choices alternatives = new Choices(changeGrammar, setGrammar);

            GrammarBuilder result = new GrammarBuilder(alternatives);
            result.Append(brightnessGrammar);
            result.Append(toGrammar);
            result.Append(resultContrast);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Set Brightness";
            return grammar;
        }

        private Grammar ContrastGrammar()
        {
            // Change/Set Contrast to Choices
            var choices = new Choices();
            for (var i = -100; i <= 100; i++)
            {
                SemanticResultValue choiceResultValue = new SemanticResultValue(i.ToString(), i);
                GrammarBuilder resultValueBuilder = new GrammarBuilder(choiceResultValue);
                choices.Add(resultValueBuilder);
            }

            GrammarBuilder changeGrammar = "Change";
            GrammarBuilder setGrammar = "Set";
            GrammarBuilder contrastGrammar = "Contrast";
            GrammarBuilder toGrammar = "To";

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
            GrammarBuilder image = "Image";

            Choices commands = new Choices(invert);
            SemanticResultKey resultKey = new SemanticResultKey("invert", commands);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(image);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Invert Image";
            return grammar;
        }

        private Grammar ColorFilterGrammar()
        {
            // Add/Set Filter Choices
            GrammarBuilder addGrammar = "Add";
            GrammarBuilder setGrammar = "Set";
            GrammarBuilder filterGrammar = "Filter";

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
            result.Append(resultContrast);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Set Contrast";
            return grammar;
        }

        private Grammar FlipGrammar()
        {
            // Flip Image
            GrammarBuilder flip = "Flip";
            GrammarBuilder image = "Image";

            Choices commands = new Choices(flip);
            SemanticResultKey resultKey = new SemanticResultKey("flip", commands);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(image);

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
            // Undo Actions
            GrammarBuilder undo = "Undo";
            GrammarBuilder action = "Action";

            Choices commands = new Choices(undo);

            SemanticResultKey resultKey = new SemanticResultKey("undo", commands);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(action);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Undo Action";
            return grammar;
        }
    }
}
