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

            Grammar flipGrammar = FlipGrammar();
            flipGrammar.Enabled = true;
            engine.LoadGrammar(flipGrammar);

            Grammar redoGrammar = UndoGrammar();
            redoGrammar.Enabled = true; 
            engine.LoadGrammar(redoGrammar);

            engine.RecognizeAsync(RecognizeMode.Multiple);
            engine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(engineSpeechRecognizer);

            synthesizer.Speak("Application ready.");

        }

        void engineSpeechRecognizer(object sender, SpeechRecognizedEventArgs e)
        {
            SemanticValue semantics = e.Result.Semantics;
            //string rawText = e.Result.Text;
            //RecognitionResult result = e.Result();

            if (semantics.ContainsKey("undo"))
            {
                if (imageList.Count > 1) {
                    imageList.RemoveAt(imageList.Count - 1);
                } else {
                    synthesizer.Speak("There is no more redo actions.");
                }
            }
            else if (semantics.ContainsKey("flip"))
            {
                Image lastImage = LastImage();
                Image tmpImage = (Image) lastImage.Clone();
                tmpImage.RotateFlip(RotateFlipType.Rotate180FlipY);
                imageList.Add(tmpImage);
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

        private Grammar FlipGrammar()
        {
            GrammarBuilder flip = "flip";

            Choices commands = new Choices(flip);

            SemanticResultKey resultKey = new SemanticResultKey("flip", commands);

            GrammarBuilder image = "image";

            Choices text = new Choices(image);

            GrammarBuilder result = new GrammarBuilder(resultKey);
            result.Append(text);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Flip imagen";
            return grammar;
        }

        private Grammar UndoGrammar()
        {
            GrammarBuilder undo = "undo";

            Choices commands = new Choices(undo);

            SemanticResultKey resultKey = new SemanticResultKey("undo", commands);

            GrammarBuilder result = new GrammarBuilder(resultKey);

            Grammar grammar = new Grammar(result);
            grammar.Name = "Undo";
            return grammar;
        }

    }
}
