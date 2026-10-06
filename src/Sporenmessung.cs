using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle(MarioSporen.AppIdentity.Name)]
[assembly: System.Reflection.AssemblyProduct(MarioSporen.AppIdentity.Name)]
[assembly: System.Reflection.AssemblyVersion(MarioSporen.AppIdentity.Version)]
[assembly: System.Reflection.AssemblyFileVersion(MarioSporen.AppIdentity.Version)]
[assembly: System.Reflection.AssemblyInformationalVersion(MarioSporen.AppIdentity.Version)]

namespace MarioSporen
{
    public static class AppIdentity
    {
        public const string Name = "Trinos Sporenmesser";
        public const string Version = "1.5.1026.2";
        public const string WindowTitle = Name;
    }

    public class NumberOptions
    {
        public string Separator { get; set; }
        public int LengthDigits { get; set; }
        public int QuotientDigits { get; set; }
        public int VolumeDigits { get; set; }
        public bool ShowImageMeasures { get; set; }
        public NumberOptions() { Separator=",";LengthDigits=QuotientDigits=VolumeDigits=2; }
        public NumberOptions Copy() { return (NumberOptions)MemberwiseClone(); }
        public void Validate()
        {
            if((Separator!=","&&Separator!=".")||new[]{LengthDigits,QuotientDigits,VolumeDigits}.Any(d=>d<0||d>2))
                throw new InvalidDataException("Ungültige Zahlenoptionen. Erlaubt sind Komma oder Punkt und 0, 1 oder 2 Nachkommastellen.");
        }
        public static string FilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TrinosSporenmesser","optionen.json"); } }
        public void Save(string path)
        {
            Validate();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            Document.WriteAtomic(path,Encoding.UTF8.GetBytes(Document.Serializer().Serialize(this)));
        }
        public static NumberOptions Load(string path)
        {
            if(!File.Exists(path))return new NumberOptions();
            if(new FileInfo(path).Length>4096)throw new InvalidDataException("Die Optionsdatei ist ungültig.");
            var options=Document.Serializer().Deserialize<NumberOptions>(File.ReadAllText(path,Encoding.UTF8));
            if(options==null)throw new InvalidDataException("Die Optionsdatei ist leer.");options.Validate();return options;
        }
    }
    public static class NumberDisplay
    {
        public static NumberOptions Current = new NumberOptions();
        public static int Digits(int column) { return column<2?Current.LengthDigits:column==2?Current.QuotientDigits:Current.VolumeDigits; }
        public static NumberFormatInfo Format
        {
            get { var f=(NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();f.NumberDecimalSeparator=Current.Separator;return f; }
        }
        public static string Value(double value,int column,bool trim=false)
        {
            int digits=Digits(column);
            return value.ToString(trim?(digits==0?"0":"0."+new string('#',digits)):"F"+digits,Format);
        }
        public static string Limit(double value,int column,bool upper,bool trim=false)
        {
            double factor=Math.Pow(10,Digits(column));
            return Value((upper?Math.Ceiling(value*factor):Math.Floor(value*factor))/factor,column,trim);
        }
        public static string Detail(double value,string pattern) { return value.ToString(pattern,Format); }
    }
    public class OptionsDialog : Form
    {
        private readonly ComboBox separator=new ComboBox(),length=new ComboBox(),quotient=new ComboBox(),volume=new ComboBox();
        private readonly Label preview=new Label();
        private readonly CheckBox imageMeasures=new CheckBox {Text="Maße am Bild anzeigen (auch JPG / PNG)",AutoSize=true};
        public NumberOptions Options
        {
            get { return new NumberOptions {Separator=separator.SelectedIndex==0?",":".",LengthDigits=length.SelectedIndex,QuotientDigits=quotient.SelectedIndex,VolumeDigits=volume.SelectedIndex,ShowImageMeasures=imageMeasures.Checked}; }
        }
        public OptionsDialog(NumberOptions options)
        {
            Text="Optionen";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;
            MinimizeBox=false;MaximizeBox=false;ShowInTaskbar=false;AutoScaleMode=AutoScaleMode.Dpi;
            Font=new Font("Segoe UI",10);BackColor=Color.White;ClientSize=new Size(470,410);
            var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(22),ColumnCount=2,RowCount=8};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));
            for(int i=0;i<4;i++)layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,45));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));
            ComboBox[] boxes={separator,length,quotient,volume};string[] labels={"Dezimaltrennzeichen","Nachkommastellen L und B","Nachkommastellen Q","Nachkommastellen Volumen"};
            for(int i=0;i<4;i++)
            {
                layout.Controls.Add(new Label {Text=labels[i],AutoSize=true,Anchor=AnchorStyles.Left},0,i);
                boxes[i].DropDownStyle=ComboBoxStyle.DropDownList;boxes[i].Dock=DockStyle.Fill;boxes[i].AccessibleName=labels[i];
                boxes[i].Items.AddRange(i==0?new object[]{"Komma (1,23)","Punkt (1.23)"}:new object[]{"Keine (0)","1","2"});layout.Controls.Add(boxes[i],1,i);
            }
            separator.SelectedIndex=options.Separator==","?0:1;length.SelectedIndex=options.LengthDigits;quotient.SelectedIndex=options.QuotientDigits;volume.SelectedIndex=options.VolumeDigits;
            preview.Dock=DockStyle.Fill;preview.TextAlign=ContentAlignment.MiddleLeft;preview.ForeColor=Color.Blue;layout.Controls.Add(preview,0,4);layout.SetColumnSpan(preview,2);
            imageMeasures.Checked=options.ShowImageMeasures;imageMeasures.Anchor=AnchorStyles.Left;layout.Controls.Add(imageMeasures,0,5);layout.SetColumnSpan(imageMeasures,2);
            var note=new Label {Text="Zahlenformat für Anzeige und Export. Die Messwerte bleiben intern ungerundet. „Übernehmen“ speichert die Auswahl dauerhaft.",Dock=DockStyle.Fill};layout.Controls.Add(note,0,6);layout.SetColumnSpan(note,2);
            var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};
            var ok=new Button {Text="Übernehmen",AutoSize=true,DialogResult=DialogResult.OK};var cancel=new Button {Text="Abbrechen",AutoSize=true,DialogResult=DialogResult.Cancel};buttons.Controls.Add(ok);buttons.Controls.Add(cancel);layout.Controls.Add(buttons,0,7);layout.SetColumnSpan(buttons,2);
            AcceptButton=ok;CancelButton=cancel;Controls.Add(layout);
            Action update=delegate {
                var o=Options;var f=(NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();f.NumberDecimalSeparator=o.Separator;
                preview.Text="Beispiel:  L "+12.345.ToString("F"+o.LengthDigits,f)+" · Q "+1.2345.ToString("F"+o.QuotientDigits,f)+" · V "+123.456.ToString("F"+o.VolumeDigits,f);
            };
            foreach(var box in boxes)box.SelectedIndexChanged+=delegate{update();};update();
        }
    }

    public static class Statistics
    {
        private static double LogGamma(double z)
        {
            double[] c={676.5203681218851,-1259.1392167224028,771.32342877765313,-176.61502916214059,12.507343278686905,-0.13857109526572012,9.9843695780195716e-6,1.5056327351493116e-7};
            z-=1;double x=0.99999999999980993;
            for(int i=0;i<c.Length;i++)x+=c[i]/(z+i+1);
            double t=z+7.5;return .9189385332046727+(z+.5)*Math.Log(t)-t+Math.Log(x);
        }
        private static double BetaFraction(double a,double b,double x)
        {
            const double tiny=1e-300;
            double c=1,d=1-(a+b)*x/(a+1);if(Math.Abs(d)<tiny)d=tiny;d=1/d;double h=d;
            for(int m=1;m<=300;m++)
            {
                double aa=m*(b-m)*x/((a+2*m-1)*(a+2*m));
                d=1+aa*d;if(Math.Abs(d)<tiny)d=tiny;c=1+aa/c;if(Math.Abs(c)<tiny)c=tiny;d=1/d;h*=d*c;
                aa=-(a+m)*(a+b+m)*x/((a+2*m)*(a+2*m+1));
                d=1+aa*d;if(Math.Abs(d)<tiny)d=tiny;c=1+aa/c;if(Math.Abs(c)<tiny)c=tiny;d=1/d;
                double delta=d*c;h*=delta;if(Math.Abs(delta-1)<3e-14)return h;
            }
            throw new InvalidOperationException("Die Mittelwertgrenzen konnten nicht berechnet werden.");
        }
        private static double BetaRegularized(double x,double a,double b)
        {
            if(x<=0)return 0;if(x>=1)return 1;
            double factor=Math.Exp(LogGamma(a+b)-LogGamma(a)-LogGamma(b)+a*Math.Log(x)+b*Math.Log(1-x));
            if(x<(a+1)/(a+b+2))return factor*BetaFraction(a,b,x)/a;
            return 1-factor*BetaFraction(b,a,1-x)/b;
        }
        // Two-sided 95% Student t interval; df = number of observations minus one.
        public static double Critical95(int n)
        {
            if(n<2)throw new ArgumentOutOfRangeException("n");
            double df=n-1,lo=0,hi=1;
            while(BetaRegularized(df/(df+hi*hi),df/2,.5)>.05)hi*=2;
            for(int i=0;i<70;i++)
            {
                double mid=(lo+hi)/2;
                if(BetaRegularized(df/(df+mid*mid),df/2,.5)>.05)lo=mid;else hi=mid;
            }
            return (lo+hi)/2;
        }
        // Gerd Fischer workbook, Sporen!B18:E18 / B22:E22: mean +/- t*s.
        // Approximate population limits, not an exact 95/95 tolerance interval.
        public static double[] FischerPopulationLimits(double[] values,double critical)
        {
            if(values.Length<5)throw new ArgumentException("Mindestens fünf Messungen erforderlich.");
            double mean=values.Average(),squares=values.Sum(v=>(v-mean)*(v-mean));
            double margin=critical*Math.Sqrt(squares/(values.Length-1));
            return new[]{mean-margin,mean+margin};
        }
        public static double[] MeanLimits(double[] values,double critical)
        {
            if(values.Length<5)throw new ArgumentException("Mindestens fünf Messungen erforderlich.");
            double mean=values.Average(),squares=values.Sum(v=>(v-mean)*(v-mean));
            double margin=critical*Math.Sqrt(squares/(values.Length-1)/values.Length);
            return new[]{mean-margin,mean+margin};
        }
    }
    public class Spore
    {
        public int id { get; set; }
        public bool isSpike { get; set; }
        public double cx { get; set; }
        public double cy { get; set; }
        public double length { get; set; }
        public double width { get; set; }
        public double angle { get; set; }
        public double lengthLabelX { get; set; }
        public double lengthLabelY { get; set; }
        public double widthLabelX { get; set; }
        public double widthLabelY { get; set; }
        public Spore Copy() { return (Spore)MemberwiseClone(); }
        public PointF Point(double x, double y)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle);
            return new PointF((float)(cx + c*x - s*y), (float)(cy + s*x + c*y));
        }
        public PointF Local(PointF p)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle), dx = p.X-cx, dy = p.Y-cy;
            return new PointF((float)(dx*c+dy*s), (float)(-dx*s+dy*c));
        }
        public PointF[] Corners() { return new[] { Point(-length/2,-width/2), Point(length/2,-width/2), Point(length/2,width/2), Point(-length/2,width/2) }; }
    }
    public class ImageData
    {
        public string name { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string data { get; set; }
    }
    public class DisplayData
    {
        public string color { get; set; }
        public int lineWidth { get; set; }
        public bool numbers { get; set; }
    }
    public class CalibrationData
    {
        public double x1 { get; set; }
        public double y1 { get; set; }
        public double x2 { get; set; }
        public double y2 { get; set; }
        public double micrometers { get; set; }
        public CalibrationData Copy() { return (CalibrationData)MemberwiseClone(); }
        public double Pixels() { return Math.Sqrt(Math.Pow(x2-x1,2)+Math.Pow(y2-y1,2)); }
        public double Scale() { return micrometers/Pixels(); }
        public static void Validate(CalibrationData c)
        {
            if(c==null)return;
            if(!new[]{c.x1,c.y1,c.x2,c.y2,c.micrometers}.All(Document.Finite)||c.micrometers<=0||c.Pixels()<1||!Document.Finite(c.Scale())||c.Scale()<=0)
                throw new InvalidDataException("Ungültige Kalibrierung: Referenz mindestens 1 px, Länge größer als 0 µm.");
        }
    }
    public class CalibrationPreset
    {
        public string format { get; set; }
        public int version { get; set; }
        public int imageWidth { get; set; }
        public int imageHeight { get; set; }
        public string sourceImage { get; set; }
        public CalibrationData calibration { get; set; }
        public CalibrationPreset Copy()
        {
            return new CalibrationPreset {format=format,version=version,imageWidth=imageWidth,imageHeight=imageHeight,sourceImage=sourceImage,calibration=calibration==null?null:calibration.Copy()};
        }
        public bool Matches(Document doc) { return doc.Image!=null&&doc.Image.Width==imageWidth&&doc.Image.Height==imageHeight; }
        public static void Validate(CalibrationPreset preset)
        {
            if(preset==null||preset.format!="marios-sporenreferenz"||preset.version!=1||preset.imageWidth<1||preset.imageHeight<1||(long)preset.imageWidth*preset.imageHeight>100000000||preset.calibration==null)
                throw new InvalidDataException("Keine gültige Referenzdatei. Eine gespeicherte Messung bitte mit „Messung öffnen“ laden.");
            CalibrationData.Validate(preset.calibration);
        }
        public static CalibrationPreset FromDocument(Document doc)
        {
            if(doc.Image==null||doc.Calibration==null)throw new InvalidOperationException("Bitte zuerst eine Referenz kalibrieren oder eine kalibrierte Messung öffnen.");
            var preset=new CalibrationPreset {format="marios-sporenreferenz",version=1,imageWidth=doc.Image.Width,imageHeight=doc.Image.Height,sourceImage=doc.Name,calibration=doc.Calibration.Copy()};
            Validate(preset);return preset;
        }
        public void Save(string file)
        {
            Validate(this);Document.WriteAtomic(file,new UTF8Encoding(false).GetBytes(Document.Serializer().Serialize(this)));
        }
        public static CalibrationPreset Load(string file)
        {
            if(new FileInfo(file).Length>1024*1024)throw new InvalidDataException("Die Datei ist zu groß für eine Referenzdatei. Bitte die gespeicherte .sporen-referenz.json auswählen.");
            var preset=Document.Serializer().Deserialize<CalibrationPreset>(File.ReadAllText(file,Encoding.UTF8));Validate(preset);return preset;
        }
    }
    public class ReferenceSession
    {
        public CalibrationPreset Preset { get; private set; }
        public bool Reuse;
        public void Use(CalibrationPreset preset)
        {
            CalibrationPreset.Validate(preset);Preset=preset.Copy();Reuse=true;
        }
        public bool TryApply(Document doc)
        {
            if(!Reuse||Preset==null||doc.Calibration!=null||!Preset.Matches(doc))return false;
            doc.SetCalibration(Preset.calibration);return true;
        }
    }
    public class ProjectData
    {
        public string format { get; set; }
        public int version { get; set; }
        public ImageData image { get; set; }
        public List<Spore> rectangles { get; set; }
        public DisplayData display { get; set; }
        public CalibrationData calibration { get; set; }
    }
    public class Snapshot
    {
        public List<Spore> Rects;
        public int Selected;
        public CalibrationData Calibration;
    }
    public class Document : IDisposable
    {
        public Bitmap Image;
        public string ImageSource = "", Name = "", ImagePath = "";
        public List<Spore> Rects = new List<Spore>();
        public int Selected;
        public bool Dirty;
        public Func<int> NumberProvider;
        public Action<Document> AfterRestore;
        public CalibrationData Calibration;
        public double Scale { get { return Calibration==null?1:Calibration.Scale(); } }
        public string Unit { get { return Calibration==null?"px":"µm"; } }
        public double Measure(double pixels) { return pixels*Scale; }
        public string MeasureText(double pixels) { return NumberDisplay.Value(Measure(pixels),0); }
        public string RoundedMeasureText(double pixels) { return NumberDisplay.Value(Measure(pixels),0); }
        public Document Filtered(bool spikes)
        {
            return new Document {Calibration=Calibration==null?null:Calibration.Copy(),Rects=Rects.Where(r=>r.isSpike==spikes).Select(r=>r.Copy()).ToList()};
        }
        public string SpikeSummaryText()
        {
            var values=Rects.Where(r=>r.isSpike).Select(r=>Measure(r.length)).ToArray();
            if(values.Length==0)return "Stacheln\nNoch keine Stacheln gemessen.";
            var text=new StringBuilder("Stacheln · n = "+values.Length+"\nMin–Max: "+NumberDisplay.Value(values.Min(),0)+"–"+NumberDisplay.Value(values.Max(),0)+" "+Unit+"\n");
            if(values.Length>=5)
            {
                var limits=Statistics.MeanLimits(values,Statistics.Critical95(values.Length));
                text.Append("Mittelwertgrenzen (95 %)\nL ").Append(NumberDisplay.Limit(limits[0],0,false)).Append('–').Append(NumberDisplay.Value(values.Average(),0)).Append('–').Append(NumberDisplay.Limit(limits[1],0,true)).Append(' ').Append(Unit);
            }
            else text.Append("Mittel\nL ").Append(NumberDisplay.Value(values.Average(),0)).Append(' ').Append(Unit).Append("\nGrenzen ab 5 Stacheln.");
            return text.ToString();
        }
        private void ExportWithSpikes(string file,bool csv)
        {
            var encoding=csv?(Encoding)new UTF8Encoding(true):Encoding.GetEncoding(1252);
            var output=new StringBuilder();
            using(var spores=Filtered(false))
            {
                if(spores.Rects.Count>0)
                {
                    string temp=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+(csv?".csv":".txt"));
                    try { if(csv)spores.Csv(temp);else spores.Txt(temp);output.Append(File.ReadAllText(temp,encoding)).Append("\r\n"); }
                    finally { if(File.Exists(temp))File.Delete(temp); }
                }
            }
            string sep=csv?";":"\t",pad=csv?";;;":"";
            output.Append("Stacheln").Append(csv?";;;;":"").Append("\r\nNr.").Append(sep).Append("Länge [").Append(Unit).Append(']').Append(pad).Append("\r\n");
            var spikes=Rects.Where(r=>r.isSpike).OrderBy(r=>r.id).ToList();var values=spikes.Select(r=>Measure(r.length)).ToArray();
            foreach(var r in spikes)output.Append(r.id).Append(sep).Append(NumberDisplay.Value(Measure(r.length),0,true)).Append(pad).Append("\r\n");
            output.Append("n").Append(sep).Append(values.Length).Append(pad).Append("\r\n");
            output.Append("min").Append(sep).Append(NumberDisplay.Value(values.Min(),0,true)).Append(pad).Append("\r\n");
            output.Append("max").Append(sep).Append(NumberDisplay.Value(values.Max(),0,true)).Append(pad).Append("\r\n");
            output.Append("mittel").Append(sep).Append(NumberDisplay.Value(values.Average(),0,true)).Append(pad).Append("\r\n");
            var limits=values.Length>=5?Statistics.MeanLimits(values,Statistics.Critical95(values.Length)):null;
            output.Append("MW unten (95 %)").Append(sep).Append(limits==null?"n.a.":NumberDisplay.Limit(limits[0],0,false,true)).Append(pad).Append("\r\n");
            output.Append("MW oben (95 %)").Append(sep).Append(limits==null?"n.a.":NumberDisplay.Limit(limits[1],0,true,true)).Append(pad).Append("\r\n");
            if(csv)output.Append("Stachelmessung;Gerade Strecke vom Ansatz bis zur Spitze im Bild. Getrennt von Sporenmaßen ausgewertet, ohne B, Q oder Volumen.;;;\r\n");
            WriteAtomic(file,encoding.GetPreamble().Concat(encoding.GetBytes(output.ToString())).ToArray());
        }
        public string CompactSummaryText()
        {
            if(Rects.Any(r=>r.isSpike)){using(var spores=Filtered(false))return spores.CompactSummaryText();}
            if(Rects.Count==0)return "";
            var columns=new[]{Rects.Select(r=>Measure(r.length)).ToArray(),Rects.Select(r=>Measure(r.width)).ToArray(),Rects.Select(r=>r.length/r.width).ToArray()};
            bool limitsAvailable=Rects.Count>=5;double critical=limitsAvailable?Statistics.Critical95(Rects.Count):0;
            var parts=new string[3];
            for(int i=0;i<3;i++)
            {
                string mean=NumberDisplay.Value(columns[i].Average(),i);
                if(limitsAvailable)
                {
                    var limits=Statistics.MeanLimits(columns[i],critical);
                    parts[i]=NumberDisplay.Limit(limits[0],i,false)+"-"+mean+"-"+NumberDisplay.Limit(limits[1],i,true);
                }
                else parts[i]=mean;
            }
            string heading=limitsAvailable?"Zusammenfassung (95-%-Mittelwertgrenzen, n = "+Rects.Count+")":"Zusammenfassung (Mittelwerte, n = "+Rects.Count+"; Grenzen ab 5 Sporen)";
            return heading+"\r\nSporen: "+parts[0]+" x "+parts[1]+" "+Unit+"\r\nQ: "+parts[2]+"\r\n";
        }
        public string SummaryText()
        {
            if(Rects.Any(r=>r.isSpike)){using(var spores=Filtered(false))return spores.SummaryText();}
            if(Rects.Count==0)return "L und B in "+Unit+".\nQ = Länge / Breite.";
            var text=new StringBuilder("Auswertung\nMittelwertgrenzen (95 %)\n");
            double critical=Rects.Count>=5?Statistics.Critical95(Rects.Count):0;
            var columns=new[]{Rects.Select(r=>Measure(r.length)).ToArray(),Rects.Select(r=>Measure(r.width)).ToArray(),Rects.Select(r=>r.length/r.width).ToArray()};
            string[] labels={"L","B","Q"};
            for(int i=0;i<3;i++)
            {
                text.Append(labels[i]).Append(' ');
                if(Rects.Count>=5)
                {
                    var limits=Statistics.MeanLimits(columns[i],critical);
                    text.Append(NumberDisplay.Limit(limits[0],i,false)).Append('–');
                    text.Append(NumberDisplay.Value(columns[i].Average(),i)).Append('–');
                    text.Append(NumberDisplay.Limit(limits[1],i,true));
                }
                else text.Append(NumberDisplay.Value(columns[i].Average(),i));
                if(i<2)text.Append(' ').Append(Unit);
                if(i<2)text.Append('\n');
            }
            if(Rects.Count<5)text.Append("\nGrenzen ab 5 Sporen.");
            return text.ToString();
        }

        public Color LineColor = Color.FromArgb(255,64,87);
        public int LineWidth = 1;
        public bool Numbers = true;
        public List<Snapshot> Past = new List<Snapshot>(), Future = new List<Snapshot>();
        public Spore Selection { get { return Rects.Find(r => r.id == Selected); } }
        public int NextNumber { get { if(NumberProvider!=null)return NumberProvider();var ids = new HashSet<int>(Rects.Select(r=>r.id)); int n=1; while(ids.Contains(n))n++; return n; } }
        public Snapshot Capture() { return new Snapshot { Rects = Rects.Select(r=>r.Copy()).ToList(), Selected = Selected, Calibration=Calibration==null?null:Calibration.Copy() }; }
        public void Restore(Snapshot s) { Rects = s.Rects.Select(r=>r.Copy()).ToList(); Selected=s.Selected; Calibration=s.Calibration==null?null:s.Calibration.Copy();if(AfterRestore!=null)AfterRestore(this); }
        public void SetCalibration(CalibrationData value) { CalibrationData.Validate(value);var before=Capture();Calibration=value==null?null:value.Copy();Commit(before); }
        public void Commit(Snapshot before) { Past.Add(before); if(Past.Count>150)Past.RemoveAt(0); Future.Clear(); Dirty=true; }
        public void Delete() { if(Selection==null)return; var s=Capture(); Rects.Remove(Selection); Selected=0; Commit(s); }
        public void Undo() { if(Past.Count==0)return; Future.Add(Capture()); var s=Past[Past.Count-1]; Past.RemoveAt(Past.Count-1); Restore(s); Dirty=true; }
        public void Redo() { if(Future.Count==0)return; Past.Add(Capture()); var s=Future[Future.Count-1]; Future.RemoveAt(Future.Count-1); Restore(s); Dirty=true; }
        public void Add(Spore r) { var before=Capture(); r.id=NextNumber; Rects.Add(r); Selected=r.id; Commit(before); }
        public static double Normalize(double a) { return Math.Atan2(Math.Sin(a),Math.Cos(a)); }
        public static bool Finite(double v) { return !double.IsNaN(v)&&!double.IsInfinity(v)&&Math.Abs(v)<10000000; }
        public static Bitmap Decode(string source)
        {
            if(source==null||!source.StartsWith("data:image/",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Die Messdatei enthält kein gültiges Bild.");
            int split=source.IndexOf(";base64,",StringComparison.Ordinal);
            if(split<0)throw new InvalidDataException("Das Bildformat der Messdatei ist ungültig.");
            byte[] bytes=Convert.FromBase64String(source.Substring(split+8));
            using(var ms=new MemoryStream(bytes))using(var img=System.Drawing.Image.FromStream(ms,true,true))
            {
                if((long)img.Width*img.Height>100000000)throw new InvalidDataException("Das Bild ist zu groß (maximal 100 Megapixel).");
                return new Bitmap(img);
            }
        }
        public void Install(string source,string name,List<Spore> rects,DisplayData display,string originalPath)
        {
            var image=Decode(source);
            if(Image!=null)Image.Dispose(); Image=image; ImageSource=source; Name=name; ImagePath=originalPath??"";
            Rects=rects??new List<Spore>(); Selected=0; Calibration=null; Past.Clear(); Future.Clear(); Dirty=false;
            if(display!=null){LineColor=ColorTranslator.FromHtml(display.color); LineWidth=display.lineWidth; Numbers=display.numbers;}
        }
        public void LoadImage(string file)
        {
            string ext=Path.GetExtension(file).ToLowerInvariant(),mime=ext==".jpg"||ext==".jpeg"?"jpeg":ext==".bmp"?"bmp":ext==".tif"||ext==".tiff"?"tiff":"png";
            string data="data:image/"+mime+";base64,"+Convert.ToBase64String(File.ReadAllBytes(file));
            Install(data,Path.GetFileName(file),null,null,Path.GetFullPath(file));
        }
        public static JavaScriptSerializer Serializer() { return new JavaScriptSerializer { MaxJsonLength=512*1024*1024, RecursionLimit=64 }; }
        public static ProjectData Parse(string json)
        {
            ProjectData p=Serializer().Deserialize<ProjectData>(json);
            if(p==null||p.format!="marios-sporenmessung"||(p.version!=1&&p.version!=2&&p.version!=3)||p.image==null||string.IsNullOrEmpty(p.image.name)||p.image.width<1||p.image.height<1||p.rectangles==null||p.rectangles.Count>10000)
                throw new InvalidDataException("Keine gültige Sporen-Messdatei.");
            if(p.version<3&&p.rectangles.Any(r=>r!=null&&r.isSpike))throw new InvalidDataException("Stachelmessungen benötigen das neue Messdateiformat.");
            CalibrationData.Validate(p.calibration);
            if(p.version==2&&p.calibration==null)throw new InvalidDataException("Die kalibrierte Messdatei enthält keinen Maßstab.");
            var ids=new HashSet<int>();
            foreach(var r in p.rectangles)
                if(r==null||r.id<1||!ids.Add(r.id)||!Finite(r.cx)||!Finite(r.cy)||!Finite(r.length)||!Finite(r.width)||!Finite(r.angle)||!Finite(r.lengthLabelX)||!Finite(r.lengthLabelY)||!Finite(r.widthLabelX)||!Finite(r.widthLabelY)||r.length<.25||r.width<.25)
                    throw new InvalidDataException("Die Messdatei enthält ungültige Rechtecke.");
            if(p.display!=null&&(!System.Text.RegularExpressions.Regex.IsMatch(p.display.color??"","^#[a-fA-F0-9]{6}$")||p.display.lineWidth<1||p.display.lineWidth>2))
                throw new InvalidDataException("Ungültige Darstellungseinstellungen.");
            using(var img=Decode(p.image.data))if(img.Width!=p.image.width||img.Height!=p.image.height)throw new InvalidDataException("Bildgröße und Messdatei passen nicht zusammen.");
            return p;
        }
        public void LoadProject(string file)
        {
            var p=Parse(File.ReadAllText(file,Encoding.UTF8));
            Install(p.image.data,p.image.name,p.rectangles,p.display,"");
            Calibration=p.calibration==null?null:p.calibration.Copy();
        }
        public ProjectData Project()
        {
            if(Image==null)throw new InvalidOperationException("Bitte zuerst ein Bild öffnen.");
            return new ProjectData { format="marios-sporenmessung",version=Rects.Any(r=>r.isSpike)?3:Calibration==null?1:2,calibration=Calibration==null?null:Calibration.Copy(),
                image=new ImageData { name=Name,width=Image.Width,height=Image.Height,data=ImageSource },
                rectangles=Rects.Select(r=>r.Copy()).ToList(),
                display=new DisplayData { color=ColorTranslator.ToHtml(Color.FromArgb(LineColor.R,LineColor.G,LineColor.B)),lineWidth=LineWidth,numbers=Numbers } };
        }
        public static void WriteAtomic(string file,byte[] bytes)
        {
            string target=Path.GetFullPath(file),tmp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllBytes(tmp,bytes);if(File.Exists(target))File.Replace(tmp,target,null);else File.Move(tmp,target);}
            finally{if(File.Exists(tmp))File.Delete(tmp);}
        }
        public void Save(string file) { WriteAtomic(file,new UTF8Encoding(false).GetBytes(Serializer().Serialize(Project()))); Dirty=false; }
        public void Txt(string file)
        {
            if(Rects.Any(r=>r.isSpike)){ExportWithSpikes(file,false);return;}
            if(Rects.Count==0)throw new InvalidOperationException("Bitte zuerst eine Spore messen.");
            string divider=new string('-',51),unit=Unit;
            var sb=new StringBuilder("#\tLänge\tBreite\tQ.\tVolumen\r\n         ["+unit+"]    ["+unit+"]             ["+unit+"³]\r\n"+divider+"\r\n");
            var rows=new List<double[]>();
            Func<double,int,string> number=(v,i)=>NumberDisplay.Value(v,i,true);
            foreach(var r in Rects.OrderBy(r=>r.id))
            {
                double length=Measure(r.length),width=Measure(r.width);
                var values=new[]{length,width,r.length/r.width,Math.PI/6*length*width*width};rows.Add(values);
                sb.Append(r.id).Append('\t').Append(string.Join("\t",values.Select(number))).Append("\r\n");
            }
            sb.Append(divider).Append("\r\n\r\n");
            sb.Append("min\t").Append(string.Join("\t",Enumerable.Range(0,4).Select(i=>number(rows.Min(row=>row[i]),i)))).Append("\r\n");
            sb.Append("max\t").Append(string.Join("\t",Enumerable.Range(0,4).Select(i=>number(rows.Max(row=>row[i]),i)))).Append("\r\n");
            sb.Append("mittel\t").Append(string.Join("\t",Enumerable.Range(0,4).Select(i=>number(rows.Average(row=>row[i]),i)))).Append("\r\n");
            sb.Append("\r\n").Append(CompactSummaryText());
            // Preserve the measurement table, Windows encoding and CRLF; append the report.
            WriteAtomic(file,Encoding.GetEncoding(1252).GetBytes(sb.ToString()));
        }
        public void ExportMeasurements(string file)
        {
            string ext=Path.GetExtension(file).ToLowerInvariant();
            if(ext==".txt")Txt(file);
            else if(ext==".csv")Csv(file);
            else throw new InvalidOperationException("Bitte eine TXT- oder CSV-Dateiendung verwenden.");
        }
        public void Csv(string file)
        {
            if(Rects.Any(r=>r.isSpike)){ExportWithSpikes(file,true);return;}
            if(Rects.Count==0)throw new InvalidOperationException("Bitte zuerst eine Spore messen.");
            var sb=new StringBuilder("Nr.;Länge ["+Unit+"];Breite ["+Unit+"];Q;Geschätztes Volumen ["+Unit+"³]\r\n");
            var rows=new List<double[]>();
            Func<double,int,string> number=(v,i)=>NumberDisplay.Value(v,i,true);
            foreach(var r in Rects.OrderBy(r=>r.id))
            {
                double length=Measure(r.length),width=Measure(r.width);
                var values=new[]{length,width,r.length/r.width,Math.PI/6*length*width*width};rows.Add(values);
                sb.Append(r.id).Append(';').Append(string.Join(";",values.Select(number))).Append("\r\n");
            }
            sb.Append("min;").Append(string.Join(";",Enumerable.Range(0,4).Select(i=>number(rows.Min(row=>row[i]),i)))).Append("\r\n");
            sb.Append("max;").Append(string.Join(";",Enumerable.Range(0,4).Select(i=>number(rows.Max(row=>row[i]),i)))).Append("\r\n");
            sb.Append("mittel;").Append(string.Join(";",Enumerable.Range(0,4).Select(i=>number(rows.Average(row=>row[i]),i)))).Append("\r\n");
            var lower=new string[4];var upper=new string[4];
            var populationLower=new string[4];var populationUpper=new string[4];
            bool negativeLimit=false;
            if(rows.Count>=5)
            {
                double critical=Statistics.Critical95(rows.Count);
                for(int i=0;i<4;i++)
                {
                    var values=rows.Select(row=>row[i]).ToArray();
                    var limits=Statistics.MeanLimits(values,critical);
                    var population=Statistics.FischerPopulationLimits(values,critical);
                    lower[i]=NumberDisplay.Limit(limits[0],i,false,true);
                    upper[i]=NumberDisplay.Limit(limits[1],i,true,true);
                    populationLower[i]=NumberDisplay.Limit(population[0],i,false,true);
                    populationUpper[i]=NumberDisplay.Limit(population[1],i,true,true);
                    negativeLimit|=limits[0]<0||population[0]<0;
                }
            }
            else for(int i=0;i<4;i++)lower[i]=upper[i]=populationLower[i]=populationUpper[i]="n.a.";
            sb.Append("MW unten (95 %);").Append(string.Join(";",lower)).Append("\r\n");
            sb.Append("MW oben (95 %);").Append(string.Join(";",upper)).Append("\r\n");
            sb.Append("PG unten (95 %);").Append(string.Join(";",populationLower)).Append("\r\n");
            sb.Append("PG oben (95 %);").Append(string.Join(";",populationUpper)).Append("\r\n");
            sb.Append(";;;;\r\n");
            sb.Append("Berechnung Volumen;V = (π / 6) × L × B² (L = Länge, B = Breite);;;\r\n");
            sb.Append("Annahme;Rotationsellipsoid: Tiefe = Breite. Volumen ist geschätzt, nicht direkt gemessen.;;;\r\n");
            sb.Append("Mittelwert Volumen;Durchschnitt der einzeln berechneten Volumina, nicht Volumen aus mittlerer Länge und Breite.;;;\r\n");
            sb.Append("Berechnung MW-Grenzen;Für L, B, Q und V: Mittelwert ± t(0,975,n-1) × s / √n, ab n = 5. s = Stichproben-Standardabweichung.;;;\r\n");
            sb.Append("Berechnung PG-Grenzen;Für L, B, Q und V nach Gerd Fischer: Mittelwert ± t(0,975,n-1) × s, ab n = 5.;;;\r\n");
            sb.Append("Einordnung PG;Näherung nach Fischers Vorlage bei angenommener Normalverteilung. Kein exaktes 95/95-Toleranzintervall.;;;\r\n");
            sb.Append("Vergleich zur Vorlage;t-Wert numerisch berechnet statt gerundeter Tabellenwerte. Nachkommastellen gemäß Optionen.;;;\r\n");
            if(negativeLimit)sb.Append("Negative Untergrenze;Rechnerisches Ergebnis des symmetrischen Modells, keine physikalisch mögliche Größe. Modellannahme und Messreihe prüfen.;;;\r\n");
            sb.Append("Rundung;Berechnung mit ungerundeten Werten. Ausgabe gemäß Optionen, MW- und PG-Grenzen nach außen gerundet.;;;\r\n");
            sb.Append("Zahlenformat;Dezimaltrennzeichen: ").Append(NumberDisplay.Current.Separator).Append(" / Nachkommastellen L und B: ").Append(NumberDisplay.Current.LengthDigits).Append(" / Q: ").Append(NumberDisplay.Current.QuotientDigits).Append(" / V: ").Append(NumberDisplay.Current.VolumeDigits).Append(";;;\r\n");
            // UTF-8 BOM preserves umlauts and micrometer units in Windows spreadsheet apps.
            var encoding=new UTF8Encoding(true);
            WriteAtomic(file,encoding.GetPreamble().Concat(encoding.GetBytes(sb.ToString())).ToArray());
        }
        public void Png(string file) { ExportImage(file,false); }
        public void Jpg(string file) { ExportImage(file,true); }
        public void ExportImage(string file,bool jpeg)
        {
            if(ImagePath!=""&&string.Equals(Path.GetFullPath(file),ImagePath,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Bitte einen anderen Dateinamen wählen. Das Originalbild bleibt unverändert.");
            using(var copy=new Bitmap(Image.Width,Image.Height,jpeg?PixelFormat.Format24bppRgb:PixelFormat.Format32bppArgb))
            {
                using(var g=Graphics.FromImage(copy)){if(jpeg)g.Clear(Color.White);g.DrawImageUnscaled(Image,0,0);foreach(var r in Rects)Drawing.Rectangle(g,r,this,1,false,false);}
                using(var ms=new MemoryStream())
                {
                    if(jpeg)
                    {
                        var codec=ImageCodecInfo.GetImageEncoders().First(c=>c.FormatID==ImageFormat.Jpeg.Guid);
                        using(var options=new EncoderParameters(1)){options.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,95L);copy.Save(ms,codec,options);}
                    }
                    else copy.Save(ms,ImageFormat.Png);
                    WriteAtomic(file,ms.ToArray());
                }
            }
        }
        public void Dispose() { if(Image!=null){Image.Dispose();Image=null;} }
    }
    public class MeasureLabelLayout
    {
        public int Index;
        public string Text;
        public float X,Y,Width,Height,Angle;
        public bool Contains(PointF point)
        {
            double radians=Angle*Math.PI/180,dx=point.X-X,dy=point.Y-Y;
            return Math.Abs(dx*Math.Cos(radians)+dy*Math.Sin(radians))<=Width/2 && Math.Abs(-dx*Math.Sin(radians)+dy*Math.Cos(radians))<=Height/2;
        }
    }
    public static class Drawing
    {
        public static string MeasureLabel(Document d,Spore r,bool length)
        {
            return (length?"L ":"B ")+NumberDisplay.Value(d.Measure(length?r.length:r.width),0)+" "+d.Unit;
        }
        public static List<MeasureLabelLayout> MeasureLabels(Graphics g,Spore r,Document d)
        {
            var result=new List<MeasureLabelLayout>();
            var points=new[]{r.Point(-r.length*.15+r.lengthLabelX,-r.width/2+r.lengthLabelY),r.Point(r.length/2+r.widthLabelX,r.widthLabelY),r.Point(0,0),r.Point(1,0),r.Point(0,1)};
            using(var transform=g.Transform)transform.TransformPoints(points);
            float ux=points[3].X-points[2].X,uy=points[3].Y-points[2].Y;
            float vx=points[4].X-points[2].X,vy=points[4].Y-points[2].Y;
            float ul=(float)Math.Sqrt(ux*ux+uy*uy),vl=(float)Math.Sqrt(vx*vx+vy*vy);
            if(ul<=0||vl<=0)return result;
            var state=g.Save();
            try
            {
                g.ResetTransform();
                using(var font=new Font("Segoe UI",12,FontStyle.Bold,GraphicsUnit.Pixel))
                using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())
                {
                    format.FormatFlags=StringFormatFlags.NoWrap;
                    for(int i=0;i<(r.isSpike?1:2);i++)
                    {
                        string text=MeasureLabel(d,r,i==0);var size=g.MeasureString(text,font,1000,format);
                        float w=size.Width+8,h=size.Height+6,offset=h/2+8;
                        float nx=i==0?-vx/vl:ux/ul,ny=i==0?-vy/vl:uy/ul;
                        float angle=(float)(Math.Atan2(i==0?uy:vy,i==0?ux:vx)*180/Math.PI);
                        if(angle>90)angle-=180;else if(angle< -90)angle+=180;
                        float x=points[i].X+nx*offset,y=points[i].Y+ny*offset;
                        double radians=angle*Math.PI/180;
                        float halfW=(float)((Math.Abs(Math.Cos(radians))*w+Math.Abs(Math.Sin(radians))*h)/2);
                        float halfH=(float)((Math.Abs(Math.Sin(radians))*w+Math.Abs(Math.Cos(radians))*h)/2);
                        var clip=g.VisibleClipBounds;
                        if(x+halfW<clip.Left||x-halfW>clip.Right||y+halfH<clip.Top||y-halfH>clip.Bottom)continue;
                        if(clip.Width>=halfW*2+4)x=Math.Max(clip.Left+halfW+2,Math.Min(clip.Right-halfW-2,x));
                        if(clip.Height>=halfH*2+4)y=Math.Max(clip.Top+halfH+2,Math.Min(clip.Bottom-halfH-2,y));
                        result.Add(new MeasureLabelLayout {Index=i,Text=text,X=x,Y=y,Width=w,Height=h,Angle=angle});
                    }
                }
            }
            finally {g.Restore(state);}
            return result;
        }
        private static void Measures(Graphics g,Spore r,Document d)
        {
            var labels=MeasureLabels(g,r,d);var state=g.Save();
            try
            {
                g.ResetTransform();
                using(var font=new Font("Segoe UI",12,FontStyle.Bold,GraphicsUnit.Pixel))
                using(var background=new SolidBrush(Color.FromArgb(225,21,50,61)))
                using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())
                {
                    format.FormatFlags=StringFormatFlags.NoWrap;format.Alignment=StringAlignment.Center;format.LineAlignment=StringAlignment.Center;
                    foreach(var label in labels)
                    {
                        var labelState=g.Save();
                        try
                        {
                            g.TranslateTransform(label.X,label.Y);g.RotateTransform(label.Angle);
                            var box=new RectangleF(-label.Width/2,-label.Height/2,label.Width,label.Height);g.FillRectangle(background,box);
                            g.DrawString(label.Text,font,Brushes.White,box,format);
                        }
                        finally {g.Restore(labelState);}
                    }
                }
            }
            finally {g.Restore(state);}
        }
        public static PointF[] Handles(Spore r,double zoom) { if(r.isSpike)return new[]{r.Point(-r.length/2,0),r.Point(r.length/2,0)};return new[]{r.Point(-r.length/2,0),r.Point(r.length/2,0),r.Point(0,-r.width/2),r.Point(0,r.width/2),r.Point(0,-r.width/2-28/zoom)}; }
        public static void Rectangle(Graphics g,Spore r,Document d,double zoom,bool active,bool draft)
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(d.LineColor,(float)(d.LineWidth/zoom))){if(draft)pen.DashStyle=DashStyle.Dash;if(r.isSpike)g.DrawLine(pen,r.Point(-r.length/2,0),r.Point(r.length/2,0));else g.DrawPolygon(pen,r.Corners());}
            if(active)
            {
                var hs=Handles(r,zoom);
                if(!r.isSpike)using(var pen=new Pen(Color.White,(float)(1/zoom)))g.DrawLine(pen,r.Point(0,-r.width/2),hs[4]);
                for(int i=0;i<hs.Length;i++)
                {
                    float rad=(float)((i==4?5:4)/zoom); var box=new RectangleF(hs[i].X-rad,hs[i].Y-rad,rad*2,rad*2);
                    using(var pen=new Pen(Color.FromArgb(20,60,70),(float)(1/zoom)))
                    using(var fill=new SolidBrush(i==4?Color.FromArgb(18,104,88):Color.White))
                    {if(i==4){g.FillEllipse(fill,box);g.DrawEllipse(pen,box);}else{g.FillRectangle(fill,box);g.DrawRectangle(pen,box.X,box.Y,box.Width,box.Height);}}
                }
            }
            if(NumberDisplay.Current.ShowImageMeasures&&!draft)Measures(g,r,d);
            if(d.Numbers&&!draft)
            {
                // Draw labels at a fixed screen size, avoiding subpixel font sizes
                // under large zoom factors in GDI+ implementations such as Wine.
                var points=new[]{r.Point(-r.length/2,-r.width/2)};
                using(var transform=g.Transform)transform.TransformPoints(points);
                var state=g.Save();
                try
                {
                    g.ResetTransform();float pad=3;var p=points[0];
                    using(var font=new Font("Segoe UI",12,FontStyle.Bold,GraphicsUnit.Pixel))
                    {
                        string label=(r.isSpike?"St":"")+r.id.ToString();SizeF box=g.MeasureString(label,font);
                        using(var fill=new SolidBrush(Color.FromArgb(235,21,50,61)))g.FillRectangle(fill,p.X,p.Y-box.Height-pad*2,box.Width+pad*2,box.Height+pad*2);
                        g.DrawString(label,font,Brushes.White,p.X+pad,p.Y-box.Height-pad);
                    }
                }
                finally{g.Restore(state);}
            }
        }
    }
    public class MeasureCanvas : Control
    {
        public Document Doc;
        public Action Updated;
        public Action ViewChanged;
        public double Zoom=1, Ox=0, Oy=0;
        public bool EditMode, Space, SpikeMode;
        public bool Calibrating, ReferencePreview;
        public Action<PointF,PointF> ReferenceCompleted;
        public string DraftPhase="";
        public PointF A,B;
        public double DraftWidth;
        private string gesture="";
        private Snapshot before;
        private Spore original;
        private PointF start;
        private Point panStart;
        private double oldOx,oldOy;
        private int handle=-1,labelIndex=-1;
        private Size oldSize;
        public MeasureCanvas(Document doc)
        {
            Doc=doc; Dock=DockStyle.Fill; BackColor=Color.FromArgb(32,44,51); TabStop=true; Cursor=Cursors.Cross;
            SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.Selectable,true);
            AccessibleName="Sporenmessfläche"; AccessibleDescription="Längsachse ziehen, loslassen, Breite mit einem Klick festlegen.";
        }
        public bool IsDraft { get { return DraftPhase!=""||Calibrating||ReferencePreview; } }
        private void RefreshState() { Invalidate();if(ViewChanged!=null)ViewChanged(); if(Updated!=null)Updated(); }
        public void PanTo(double x,double y) { Ox=x;Oy=y;Invalidate();if(ViewChanged!=null)ViewChanged(); }
        public PointF ToImage(Point p) { return new PointF((float)((p.X-Ox)/Zoom),(float)((p.Y-Oy)/Zoom)); }
        public Point ToScreen(PointF p) { return new Point((int)Math.Round(p.X*Zoom+Ox),(int)Math.Round(p.Y*Zoom+Oy)); }
        public Spore Draft()
        {
            return new Spore {isSpike=SpikeMode,id=Doc.NextNumber,cx=(A.X+B.X)/2,cy=(A.Y+B.Y)/2,length=Math.Sqrt(Math.Pow(B.X-A.X,2)+Math.Pow(B.Y-A.Y,2)),width=SpikeMode?.25:DraftWidth,angle=Math.Atan2(B.Y-A.Y,B.X-A.X)};
        }
        private PointF Bounded(PointF p) { return new PointF(Math.Max(0,Math.Min(Doc.Image.Width,p.X)),Math.Max(0,Math.Min(Doc.Image.Height,p.Y))); }
        private bool Inside(PointF p) { return p.X>=0&&p.Y>=0&&p.X<=Doc.Image.Width&&p.Y<=Doc.Image.Height; }
        private static double Distance(PointF a,PointF b) { return Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)); }
        private int HitHandle(PointF p)
        {
            if(Doc.Selection==null||(SpikeMode&&!EditMode&&!Doc.Selection.isSpike))return -1;var hs=Drawing.Handles(Doc.Selection,Zoom);
            for(int i=0;i<hs.Length;i++)if(Distance(p,hs[i])*Zoom<=9)return i;return -1;
        }
        private Spore Hit(PointF p)
        {
            for(int i=Doc.Rects.Count-1;i>=0;i--){var r=Doc.Rects[i];if(SpikeMode&&!EditMode&&!r.isSpike)continue;var q=r.Local(p);if(r.isSpike){double dx=Math.Max(0,Math.Abs(q.X)-r.length/2);if(Math.Sqrt(dx*dx+q.Y*q.Y)*Zoom<=6)return r;}else if(Math.Abs(q.X)<=r.length/2+4/Zoom&&Math.Abs(q.Y)<=r.width/2+4/Zoom)return r;}return null;
        }
        private Spore HitMeasure(Point point,out int index)
        {
            index=-1;if(!NumberDisplay.Current.ShowImageMeasures||IsDraft)return null;
            using(var g=CreateGraphics())
            {
                g.TranslateTransform((float)Ox,(float)Oy);g.ScaleTransform((float)Zoom,(float)Zoom);
                for(int i=Doc.Rects.Count-1;i>=0;i--)
                {
                    var r=Doc.Rects[i];if(SpikeMode&&!EditMode&&!r.isSpike)continue;var labels=Drawing.MeasureLabels(g,r,Doc);
                    for(int j=labels.Count-1;j>=0;j--)if(labels[j].Contains(point)){index=labels[j].Index;return r;}
                }
            }
            return null;
        }
        public void Cancel()
        {
            if(before!=null&&(gesture=="move"||gesture=="handle"||gesture=="label"))Doc.Restore(before);
            before=null;gesture="";DraftPhase="";Calibrating=false;ReferencePreview=false;Capture=false;RefreshState();
        }
        public void StartCalibration() { if(Doc.Image==null)return;Cancel();Calibrating=true;Focus();RefreshState(); }
        public void FitImage()
        {
            if(Doc.Image==null||Width<1||Height<1)return;Cancel();Zoom=Math.Max(.01,Math.Min((Width-48.0)/Doc.Image.Width,(Height-48.0)/Doc.Image.Height));
            Ox=(Width-Doc.Image.Width*Zoom)/2;Oy=(Height-Doc.Image.Height*Zoom)/2;RefreshState();
        }
        public void ZoomBy(double factor,Point center)
        {
            if(Doc.Image==null)return;double old=Zoom;Zoom=Math.Max(.01,Math.Min(40,old*factor));Ox=center.X-(center.X-Ox)*Zoom/old;Oy=center.Y-(center.Y-Oy)*Zoom/old;RefreshState();
        }
        public void Begin(Point position,MouseButtons button,bool shift)
        {
            if(Doc.Image==null)return;if(button==MouseButtons.Right){Cancel();return;}if(button!=MouseButtons.Left&&button!=MouseButtons.Middle)return;
            Focus();PointF p=ToImage(position);Capture=true;
            if(button==MouseButtons.Middle||Space){gesture="pan";panStart=position;oldOx=Ox;oldOy=Oy;return;}
            if(Calibrating){if(Inside(p)){A=B=p;DraftPhase="reference";gesture="reference";}RefreshState();return;}
            if(DraftPhase=="width"){DraftWidth=Math.Max(.25,Math.Abs(Draft().Local(p).Y)*2);Doc.Add(Draft());DraftPhase="";Capture=false;RefreshState();return;}
            var label=shift?null:HitMeasure(position,out labelIndex);
            if(label!=null){Doc.Selected=label.id;gesture="label";start=p;original=label.Copy();before=Doc.Capture();RefreshState();return;}
            handle=shift?-1:HitHandle(p);
            if(handle>=0){gesture="handle";before=Doc.Capture();original=Doc.Selection.Copy();return;}
            var hit=shift?null:Hit(p);
            if(hit!=null){Doc.Selected=hit.id;gesture="move";start=p;original=hit.Copy();before=Doc.Capture();RefreshState();return;}
            Doc.Selected=0;if((!EditMode||shift)&&Inside(p)){A=B=p;DraftWidth=0;DraftPhase="axis";gesture="axis";}RefreshState();
        }
        public void MovePointer(Point position)
        {
            if(Doc.Image==null)return;PointF p=ToImage(position);
            if(gesture=="pan"){PanTo(oldOx+position.X-panStart.X,oldOy+position.Y-panStart.Y);return;}
            if(gesture=="axis"||gesture=="reference"){B=Bounded(p);RefreshState();return;}
            if(gesture=="move"){var r=Doc.Selection;r.cx=original.cx+p.X-start.X;r.cy=original.cy+p.Y-start.Y;RefreshState();return;}
            if(gesture=="label")
            {
                p=ToImage(new Point(Math.Max(0,Math.Min(Width-1,position.X)),Math.Max(0,Math.Min(Height-1,position.Y))));
                var delta=original.Local(p);var origin=original.Local(start);var r=Doc.Selection;
                if(labelIndex==0){r.lengthLabelX=original.lengthLabelX+(delta.X-origin.X);r.lengthLabelY=original.lengthLabelY+(delta.Y-origin.Y);}
                else{r.widthLabelX=original.widthLabelX+(delta.X-origin.X);r.widthLabelY=original.widthLabelY+(delta.Y-origin.Y);}
                RefreshState();return;
            }
            if(gesture=="handle")
            {
                var r=Doc.Selection;
                if(r.isSpike)
                {
                    var endpoint=Bounded(p);var fixedPoint=original.Point(handle==0?original.length/2:-original.length/2,0);
                    var a=handle==0?endpoint:fixedPoint;var b=handle==0?fixedPoint:endpoint;
                    if(Distance(a,b)>=.25){r.cx=(a.X+b.X)/2;r.cy=(a.Y+b.Y)/2;r.length=Distance(a,b);r.angle=Math.Atan2(b.Y-a.Y,b.X-a.X);}
                }
                else if(handle==4)r.angle=Document.Normalize(Math.Atan2(p.Y-original.cy,p.X-original.cx)+Math.PI/2);
                else ResizeSide(r,original,handle,p);
                RefreshState();return;
            }
            if(DraftPhase=="width"){DraftWidth=Math.Max(.25,Math.Abs(Draft().Local(p).Y)*2);RefreshState();return;}
            int hoveredLabel;bool overLabel=HitMeasure(position,out hoveredLabel)!=null;
            Cursor=Space?Cursors.Hand:overLabel?Cursors.SizeAll:HitHandle(p)>=0?Cursors.Cross:Hit(p)!=null?Cursors.SizeAll:EditMode?Cursors.Default:Cursors.Cross;
        }
        public static void ResizeSide(Spore r,Spore o,int h,PointF p)
        {
            bool length=h<2;int sign=h%2==0?-1:1;double ux=length?Math.Cos(o.angle):-Math.Sin(o.angle),uy=length?Math.Sin(o.angle):Math.Cos(o.angle),dim=length?o.length:o.width;
            double x=o.cx-ux*dim*sign/2,y=o.cy-uy*dim*sign/2;
            double size=Math.Max(.25,((p.X-x)*ux+(p.Y-y)*uy)*sign);
            if(length)r.length=size;else r.width=size;r.cx=x+ux*size*sign/2;r.cy=y+uy*size*sign/2;
        }
        public void End(Point position)
        {
            if(gesture=="label")MovePointer(position);
            string action=gesture;gesture="";Capture=false;
            if(action=="reference")
            {
                B=Bounded(ToImage(position));DraftPhase="";
                if(Draft().length>=1)
                {
                    Calibrating=false;ReferencePreview=true;RefreshState();
                    try{if(ReferenceCompleted!=null)ReferenceCompleted(A,B);}finally{ReferencePreview=false;}
                }
            }
            else if(action=="axis"){B=Bounded(ToImage(position));if(Draft().length<1)DraftPhase="";else if(SpikeMode){Doc.Add(Draft());DraftPhase="";}else{DraftPhase="width";DraftWidth=Math.Max(1,Draft().length*.4);}}
            else if((action=="move"||action=="handle"||action=="label")&&before!=null&&original!=null)
            {
                var r=Doc.Selection;
                if(r.cx!=original.cx||r.cy!=original.cy||r.length!=original.length||r.width!=original.width||r.angle!=original.angle||r.lengthLabelX!=original.lengthLabelX||r.lengthLabelY!=original.lengthLabelY||r.widthLabelX!=original.widthLabelX||r.widthLabelY!=original.widthLabelY)Doc.Commit(before);
            }
            before=null;RefreshState();
        }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e);Begin(e.Location,e.Button,(ModifierKeys&Keys.Shift)!=0); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e);MovePointer(e.Location); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e);End(e.Location); }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e);ZoomBy(Math.Exp(e.Delta*.0015),e.Location); }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e);if(!Capture&&gesture!="")Cancel(); }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // A minimized window can temporarily collapse the canvas. Keep the last
            // usable size so restoring it does not lose half a viewport of pan.
            var form=FindForm();
            if(Width<=0||Height<=0||(form!=null&&form.WindowState==FormWindowState.Minimized))return;
            if(oldSize.Width>0&&oldSize.Height>0){Ox+=(Width-oldSize.Width)/2.0;Oy+=(Height-oldSize.Height)/2.0;}
            oldSize=Size;Invalidate();if(ViewChanged!=null)ViewChanged();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);var g=e.Graphics;
            if(Width<=0||Height<=0)return;
            if(Doc.Image==null)
            {
                if(Width<=40)return;
                using(var font=new Font("Segoe UI",18))using(var small=new Font("Segoe UI",11))using(var brush=new SolidBrush(Color.FromArgb(200,221,227)))
                {
                    TextRenderer.DrawText(g,"Ein Rechteck. Zwei Schritte.",font,new Rectangle(20,Height/2-70,Width-40,45),Color.White,TextFormatFlags.HorizontalCenter);
                    TextRenderer.DrawText(g,"Über „Bild öffnen“ ein Mikroskopbild auswählen.\nLängsachse ziehen, loslassen, Breite anklicken.",small,new Rectangle(20,Height/2-10,Width-40,80),Color.FromArgb(177,202,211),TextFormatFlags.HorizontalCenter|TextFormatFlags.WordBreak);
                }return;
            }
            var saved=g.Save();g.TranslateTransform((float)Ox,(float)Oy);g.ScaleTransform((float)Zoom,(float)Zoom);
            g.InterpolationMode=Zoom>=3?InterpolationMode.NearestNeighbor:InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(Doc.Image,new Rectangle(0,0,Doc.Image.Width,Doc.Image.Height),0,0,Doc.Image.Width,Doc.Image.Height,GraphicsUnit.Pixel);
            foreach(var r in Doc.Rects)Drawing.Rectangle(g,r,Doc,Zoom,r.id==Doc.Selected&&!IsDraft,false);
            if(DraftPhase=="reference"||ReferencePreview)
            {
                using(var pen=new Pen(Color.Cyan,(float)(2/Zoom)))g.DrawLine(pen,A,B);
                float radius=(float)(4/Zoom);foreach(var p in new[]{A,B})g.FillEllipse(Brushes.Cyan,p.X-radius,p.Y-radius,radius*2,radius*2);
            }
            if(DraftPhase=="axis"){using(var pen=new Pen(Doc.LineColor,(float)(1/Zoom))){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,A,B);}}
            else if(DraftPhase=="width")Drawing.Rectangle(g,Draft(),Doc,Zoom,false,true);
            g.Restore(saved);
        }
    }
    // Native scrollbars surround the drawing surface, never the measurements table.
    public class ImageViewport : Panel
    {
        public readonly MeasureCanvas Canvas;
        public readonly HScrollBar Horizontal=new HScrollBar();
        public readonly VScrollBar Vertical=new VScrollBar();
        private readonly Panel corner=new Panel();
        private bool syncing;
        private double horizontalStart,verticalStart;
        public ImageViewport(MeasureCanvas canvas)
        {
            Canvas=canvas;Dock=DockStyle.Fill;BackColor=SystemColors.Control;
            Canvas.Dock=DockStyle.None;
            Horizontal.AccessibleName="Bild nach links oder rechts verschieben";
            Vertical.AccessibleName="Bild nach oben oder unten verschieben";
            Controls.Add(Canvas);Controls.Add(Horizontal);Controls.Add(Vertical);Controls.Add(corner);
            Canvas.ViewChanged+=Sync;
            Horizontal.ValueChanged+=delegate{if(!syncing&&Horizontal.Enabled)Canvas.PanTo(-horizontalStart-Horizontal.Value,Canvas.Oy);};
            Vertical.ValueChanged+=delegate{if(!syncing&&Vertical.Enabled)Canvas.PanTo(Canvas.Ox,-verticalStart-Vertical.Value);};
            Sync();
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);if(Canvas==null)return;
            int top=SystemInformation.HorizontalScrollBarHeight,right=SystemInformation.VerticalScrollBarWidth;
            int w=Math.Max(0,ClientSize.Width-right),h=Math.Max(0,ClientSize.Height-top);
            Horizontal.SetBounds(0,0,w,top);Vertical.SetBounds(w,top,right,h);corner.SetBounds(w,0,right,top);
            Canvas.SetBounds(0,top,w,h);Sync();
        }
        private static double SetRange(ScrollBar bar,double imageSize,int viewportSize,double offset,bool hasImage)
        {
            double start=0,end=0;
            if(hasImage&&viewportSize>0)
            {
                // Keep 24 px around the image. Include any existing freehand pan
                // without snapping the image when zooming, resizing or restoring.
                if(imageSize+48>viewportSize){start=-24;end=imageSize-viewportSize+24;}
                else start=end=-(viewportSize-imageSize)/2;
                start=Math.Min(start,-offset);end=Math.Max(end,-offset);
            }
            int travel=(int)Math.Ceiling(Math.Max(0,end-start-1e-7));
            int page=Math.Max(1,viewportSize);
            bar.Minimum=0;bar.Maximum=travel+page-1;bar.LargeChange=page;bar.SmallChange=Math.Min(32,page);
            bar.Value=Math.Max(0,Math.Min(travel,(int)Math.Round(-offset-start)));
            bar.Enabled=hasImage&&viewportSize>0&&travel>0;
            return start;
        }
        public void Sync()
        {
            if(syncing||IsDisposed||Canvas==null)return;
            var form=FindForm();if(form!=null&&form.WindowState==FormWindowState.Minimized)return;
            syncing=true;
            try
            {
                bool image=Canvas.Doc!=null&&Canvas.Doc.Image!=null;
                horizontalStart=SetRange(Horizontal,image?Canvas.Doc.Image.Width*Canvas.Zoom:0,Canvas.Width,Canvas.Ox,image);
                verticalStart=SetRange(Vertical,image?Canvas.Doc.Image.Height*Canvas.Zoom:0,Canvas.Height,Canvas.Oy,image);
            }
            finally{syncing=false;}
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing&&Canvas!=null)Canvas.ViewChanged-=Sync;
            base.Dispose(disposing);
        }
    }
    public class CalibrationDialog : Form
    {
        private readonly NumericUpDown value=new NumericUpDown();
        public double Micrometers { get { return (double)value.Value; } }
        public CalibrationDialog(double pixels,double previous)
        {
            Text="Referenzlänge in µm";Font=new Font("Segoe UI",10);ClientSize=new Size(460,240);FormBorderStyle=FormBorderStyle.FixedDialog;
            StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;AutoScaleMode=AutoScaleMode.Dpi;
            var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(18),ColumnCount=1,RowCount=4};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,66));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            layout.Controls.Add(new Label {Dock=DockStyle.Fill,Text="Gezogene Referenz: "+NumberDisplay.Detail(pixels,"F2")+" px\nWelche tatsächliche Länge hat diese Strecke in µm?"},0,0);
            value.DecimalPlaces=6;value.Minimum=.000001m;value.Maximum=9999999m;value.Value=(decimal)Math.Max(.000001,Math.Min(9999999,previous));value.Increment=1;value.Dock=DockStyle.Fill;value.AccessibleName="Referenzlänge in Mikrometern";
            layout.Controls.Add(value,0,1);layout.Controls.Add(new Label {Dock=DockStyle.Fill,Padding=new Padding(0,8,0,0),Text="Die angegebene Länge des Maßstabsbalkens eintragen, z. B. 10 für 10 µm.",ForeColor=Color.FromArgb(80,109,120)},0,2);
            var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var ok=new Button {Text="Übernehmen",AutoSize=true,DialogResult=DialogResult.OK};var cancel=new Button {Text="Abbrechen",AutoSize=true,DialogResult=DialogResult.Cancel};
            buttons.Controls.Add(ok);buttons.Controls.Add(cancel);layout.Controls.Add(buttons,0,3);Controls.Add(layout);AcceptButton=ok;CancelButton=cancel;
            Shown+=delegate{value.Focus();value.Select(0,value.Text.Length);};
        }
    }
    public class SeriesData
    {
        public string format { get; set; }
        public int version { get; set; }
        public int activeImage { get; set; }
        public List<ProjectData> images { get; set; }
    }
    public class SeriesRow
    {
        public Document Document;
        public Spore Spore;
        public int ImageIndex;
    }
    public class MeasurementSeries : IDisposable
    {
        public readonly List<Document> Documents=new List<Document>();
        public int ActiveIndex;
        private bool changed;
        public Document Current { get { return Documents[ActiveIndex]; } }
        public bool Dirty { get { return changed||Documents.Any(d=>d.Dirty); } }
        public int ImageCount { get { return Documents.Count(d=>d.Image!=null); } }
        public MeasurementSeries() { Documents.Add(new Document());Attach(Current); }
        private void Attach(Document doc) { doc.NumberProvider=NextNumber;doc.AfterRestore=EnsureUnique; }
        public int NextNumber()
        {
            var ids=new HashSet<int>(Documents.SelectMany(d=>d.Rects).Select(r=>r.id));int n=1;while(ids.Contains(n))n++;return n;
        }
        private void EnsureUnique(Document doc)
        {
            var ids=new HashSet<int>(Documents.Where(d=>d!=doc).SelectMany(d=>d.Rects).Select(r=>r.id));
            // Restoring a deleted spore must not duplicate an ID reused in another photo.
            foreach(var r in doc.Rects)
            {
                if(ids.Contains(r.id)){int previous=r.id;r.id=NextNumber();if(doc.Selected==previous)doc.Selected=r.id;}
                ids.Add(r.id);
            }
        }
        public List<SeriesRow> Rows()
        {
            return Documents.SelectMany((d,i)=>d.Rects.Select(r=>new SeriesRow {Document=d,Spore=r,ImageIndex=i})).OrderBy(row=>row.Spore.id).ToList();
        }
        public bool MixedUnits { get { var measured=Documents.Where(d=>d.Rects.Count>0).ToList();return measured.Any(d=>d.Calibration==null)&&measured.Any(d=>d.Calibration!=null); } }
        public string MissingScaleMessage { get { return "Gemeinsame Auswertung fehlt.\nBitte noch kalibrieren:\nBild "+string.Join(", ",Documents.Select((d,i)=>new {d,i}).Where(x=>x.d.Rects.Count>0&&x.d.Calibration==null).Select(x=>(x.i+1).ToString())); } }
        public Document Aggregate()
        {
            if(MixedUnits)throw new InvalidOperationException(MissingScaleMessage);
            var result=new Document();
            bool calibrated=Documents.Any(d=>d.Rects.Count>0&&d.Calibration!=null);
            if(calibrated)result.Calibration=new CalibrationData {x1=0,y1=0,x2=1,y2=0,micrometers=1};
            foreach(var row in Rows()) {var r=row.Spore.Copy();r.length=row.Document.Measure(r.length);r.width=row.Document.Measure(r.width);result.Rects.Add(r);}
            return result;
        }
        public void AddImage(string file,ReferenceSession reference)
        {
            if(ImageCount>=100)throw new InvalidOperationException("Eine Messreihe kann bis zu 100 Bilder enthalten.");
            var next=new Document();
            try { next.LoadImage(file);reference.TryApply(next); }
            catch { next.Dispose();throw; }
            if(ImageCount==0){Current.Dispose();Documents.Clear();}
            Documents.Add(next);Attach(next);ActiveIndex=Documents.Count-1;changed=true;
        }
        public void Clear()
        {
            foreach(var doc in Documents)doc.Dispose();Documents.Clear();Documents.Add(new Document());ActiveIndex=0;Attach(Current);changed=false;
        }
        public void Load(string file,ReferenceSession reference)
        {
            var loaded=new List<Document>();int active=0;
            try
            {
                if(file.EndsWith(".json",StringComparison.OrdinalIgnoreCase))
                {
                    string json=File.ReadAllText(file,Encoding.UTF8);
                    var header=Document.Serializer().Deserialize<SeriesData>(json);
                    if(header!=null&&header.format=="marios-sporenreihe")
                    {
                        if((header.version!=1&&header.version!=2)||header.images==null||header.images.Count<1||header.images.Count>100||header.activeImage<0||header.activeImage>=header.images.Count)throw new InvalidDataException("Ungültige Messreihe.");
                        active=header.activeImage;var ids=new HashSet<int>();
                        foreach(var item in header.images)
                        {
                            var project=Document.Parse(Document.Serializer().Serialize(item));
                            if(project.rectangles.Any(r=>!ids.Add(r.id)))throw new InvalidDataException("Doppelte Sporennummer in der Messreihe.");
                            var doc=new Document();loaded.Add(doc);doc.Install(project.image.data,project.image.name,project.rectangles,project.display,"");doc.Calibration=project.calibration;
                        }
                    }
                    else { var doc=new Document();loaded.Add(doc);doc.LoadProject(file); }
                }
                else {var doc=new Document();loaded.Add(doc);doc.LoadImage(file);reference.TryApply(doc);}
            }
            catch {foreach(var doc in loaded)doc.Dispose();throw;}
            foreach(var doc in Documents)doc.Dispose();Documents.Clear();Documents.AddRange(loaded);ActiveIndex=active;changed=false;foreach(var doc in Documents)Attach(doc);
        }
        public void Save(string file)
        {
            if(ImageCount==0)throw new InvalidOperationException("Bitte zuerst ein Bild öffnen.");
            if(Documents.Count==1)Current.Save(file);
            else
            {
                var data=new SeriesData {format="marios-sporenreihe",version=Documents.Any(d=>d.Rects.Any(r=>r.isSpike))?2:1,activeImage=ActiveIndex,images=Documents.Select(d=>d.Project()).ToList()};
                Document.WriteAtomic(file,new UTF8Encoding(false).GetBytes(Document.Serializer().Serialize(data)));
            }
            changed=false;foreach(var doc in Documents)doc.Dirty=false;
        }
        private static string CsvCell(string text) { return "\""+text.Replace("\"","\"\"")+"\""; }
        public void Export(string file)
        {
            using(var aggregate=Aggregate())
            {
                // Add provenance before the single atomic write to the chosen file.
                if(Path.GetExtension(file).Equals(".csv",StringComparison.OrdinalIgnoreCase)&&ImageCount>1)
                {
                    string temp=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".csv");
                    try
                    {
                        aggregate.Csv(temp);var content=new StringBuilder(File.ReadAllText(temp,Encoding.UTF8));
                        foreach(var row in Rows())content.Append(row.Spore.isSpike?"Zuordnung Stachel ":"Zuordnung Spore ").Append(row.Spore.id).Append(';').Append(CsvCell("Bild "+(row.ImageIndex+1)+": "+row.Document.Name)).Append(";;;\r\n");
                        var encoding=new UTF8Encoding(true);Document.WriteAtomic(file,encoding.GetPreamble().Concat(encoding.GetBytes(content.ToString())).ToArray());
                    }
                    finally {if(File.Exists(temp))File.Delete(temp);}
                }
                else aggregate.ExportMeasurements(file);
            }
        }
        public void Dispose() {foreach(var doc in Documents)doc.Dispose();Documents.Clear();}
    }
    public class SummaryLabel : Label
    {
        private const string MeanPattern=@"(?m)^[LBQ] (?:-?\d+(?:[,.]\d+)?–)?(?<mean>-?\d+(?:[,.]\d+)?)";
        public bool IsMeanAt(int index)
        {
            foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(Text,MeanPattern))
            {
                var group=match.Groups["mean"];if(index>=group.Index&&index<group.Index+group.Length)return true;
            }
            return false;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            int availableWidth=ClientSize.Width-Padding.Horizontal;
            if(availableWidth<=0||ClientSize.Height<=Padding.Vertical)return;
            float y=Padding.Top;
            // Use one GDI+ text path with bounded layout dimensions. Mixing native
            // TextRenderer HDC access and GDI+ can be fragile under Wine/Mono.
            using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())
            using(var brush=new SolidBrush(ForeColor))
            using(var bold=new Font(Font,FontStyle.Bold))
            {
                format.FormatFlags=StringFormatFlags.NoWrap|StringFormatFlags.MeasureTrailingSpaces;
                format.Trimming=StringTrimming.None;
                float lineHeight=Font.GetHeight(e.Graphics)+3;
                foreach(string line in Text.Split('\n'))
                {
                    if(y>=ClientSize.Height-Padding.Bottom)break;
                    float x=Padding.Left;
                    var match=System.Text.RegularExpressions.Regex.Match(line,MeanPattern);
                    var group=match.Groups["mean"];
                    string[] parts=match.Success?new[]{line.Substring(0,group.Index),group.Value,line.Substring(group.Index+group.Length)}:new[]{line};
                    for(int i=0;i<parts.Length;i++)
                    {
                        if(parts[i].Length==0)continue;
                        float width=ClientSize.Width-Padding.Right-x;if(width<=0)break;
                        var font=parts.Length==3&&i==1?bold:Font;
                        var bounds=new RectangleF(x,y,width,lineHeight*2);
                        e.Graphics.DrawString(parts[i],font,brush,bounds,format);
                        x+=e.Graphics.MeasureString(parts[i],font,new SizeF(width,lineHeight*2),format).Width;
                    }
                    y+=lineHeight;
                }
            }
        }
    }

    public class InfoDialog : Form
    {
        public const string ContactEmail = "mario.tripodi1974@gmail.com";
        public InfoDialog()
        {
            Text="Info";StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;
            MinimizeBox=false;MaximizeBox=false;ShowInTaskbar=false;AutoScaleMode=AutoScaleMode.Dpi;
            Font=new Font("Segoe UI",10);BackColor=Color.White;ClientSize=new Size(420,250);
            var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=5};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
            layout.Controls.Add(new Label {Text=AppIdentity.Name,AutoSize=true,Font=new Font(Font.FontFamily,16,FontStyle.Bold)},0,0);
            layout.Controls.Add(new Label {Text="Version "+AppIdentity.Version,AutoSize=true},0,1);
            layout.Controls.Add(new Label {Text="Mario Tripodi",AutoSize=true},0,2);
            var email=new LinkLabel {Text=ContactEmail,AutoSize=true,AccessibleName="E-Mail an Mario Tripodi",TabIndex=0};
            email.Links.Add(0,ContactEmail.Length,"mailto:"+ContactEmail);
            email.LinkClicked+=delegate(object sender,LinkLabelLinkClickedEventArgs e)
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo((string)e.Link.LinkData) {UseShellExecute=true}); }
                catch(Exception) { MessageBox.Show(this,"Das E-Mail-Programm konnte nicht geöffnet werden. Du kannst die Adresse kopieren:\n\n"+ContactEmail,"Kontakt",MessageBoxButtons.OK,MessageBoxIcon.Information); }
            };
            layout.Controls.Add(email,0,3);
            var close=new Button {Text="Schließen",AutoSize=true,Anchor=AnchorStyles.Right,DialogResult=DialogResult.OK,TabIndex=1};
            layout.Controls.Add(close,0,4);AcceptButton=close;CancelButton=close;Controls.Add(layout);
        }
    }

    public class MainForm : Form
    {
        public readonly MeasurementSeries Series=new MeasurementSeries();
        public Document Doc { get { return Series.Current; } }
        private readonly ToolStripComboBox imagePicker=new ToolStripComboBox();
        private readonly Dictionary<Document,double[]> views=new Dictionary<Document,double[]>();
        public readonly ReferenceSession References=new ReferenceSession();
        public readonly MeasureCanvas Canvas;
        public readonly ImageViewport Viewport;
        private readonly Label guide=new Label(),selection=new Label(),ratio=new Label(),count=new Label();
        private readonly SummaryLabel means=new SummaryLabel();
        private readonly TextBox lengthBox=new TextBox(),widthBox=new TextBox(),angleBox=new TextBox();
        private readonly DataGridView table=new DataGridView();
        private readonly ComboBox measurementView=new ComboBox();
        private ToolStripButton spikes;
        private int lastSelected;
        private Document lastDocument;
        private readonly ToolStripStatusLabel status=new ToolStripStatusLabel();
        private readonly ToolStripButton open,save,undo,redo,draw,edit,delete,png,txt;
        private readonly ToolStripLabel zoomLabel=new ToolStripLabel();
        private readonly ToolStripComboBox lineWidth=new ToolStripComboBox();
        private readonly ToolStripButton numbers=new ToolStripButton("Nummern");
        private readonly ToolStripButton calibrate,removeCalibration;
        private readonly ToolStripButton saveReference,loadReference,reuseReference;
        private readonly ToolStripLabel calibrationStatus=new ToolStripLabel();
        private readonly Label[] fieldLabels=new Label[3];
        private bool updating;
        private string projectPath="";
        public MainForm()
        {
            Text=AppIdentity.WindowTitle;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(1220,780);MinimumSize=new Size(1000,720);
            Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;BackColor=Color.White;KeyPreview=true;
            var strip=new ToolStrip {GripStyle=ToolStripGripStyle.Hidden,Padding=new Padding(10,8,10,8),BackColor=Color.White,Font=Font};
            open=Button(strip,"Bild öffnen",delegate{OpenImage();});
            strip.Items.Add(new ToolStripSeparator());Button(strip,"Messung öffnen",delegate{OpenProject();});save=Button(strip,"Messung speichern",delegate{SaveProject(false);});
            Button(strip,"Speichern unter…",delegate{SaveProject(true);});strip.Items.Add(new ToolStripSeparator());png=Button(strip,"Bildkopie speichern",delegate{Export(false);});txt=Button(strip,"Messwerte speichern",delegate{Export(true);});
            var info=Button(strip,"Info",delegate{using(var dialog=new InfoDialog())dialog.ShowDialog(this);});info.Alignment=ToolStripItemAlignment.Right;info.Overflow=ToolStripItemOverflow.Never;
            var help=Button(strip,"Hilfe",delegate{Help();});help.Alignment=ToolStripItemAlignment.Right;
            var options=Button(strip,"Optionen",delegate{ShowOptions();});options.Alignment=ToolStripItemAlignment.Right;options.Overflow=ToolStripItemOverflow.Never;
            var tools=new ToolStrip {GripStyle=ToolStripGripStyle.Hidden,Padding=new Padding(10,6,10,6),BackColor=Color.FromArgb(243,247,248),Font=Font};
            draw=Button(tools,"＋ Spore messen",delegate{SetMode(false);});edit=Button(tools,"Korrigieren",delegate{SetMode(true);});
            tools.Items.Add(new ToolStripSeparator());undo=Button(tools,"↶ Rückgängig",delegate{Cancel();Doc.Undo();RefreshUi();});redo=Button(tools,"↷",delegate{Cancel();Doc.Redo();RefreshUi();});
            delete=Button(tools,"Löschen",delegate{Cancel();Doc.Delete();RefreshUi();});tools.Items.Add(new ToolStripSeparator());
            Button(tools,"Linienfarbe",delegate{using(var dlg=new ColorDialog {Color=Doc.LineColor})if(dlg.ShowDialog(this)==DialogResult.OK){Doc.LineColor=dlg.Color;Doc.Dirty=Doc.Image!=null;RefreshUi();}});
            lineWidth.DropDownStyle=ComboBoxStyle.DropDownList;lineWidth.AutoSize=false;lineWidth.Width=65;lineWidth.Items.AddRange(new object[]{"1 px","2 px"});lineWidth.SelectedIndex=0;tools.Items.Add(lineWidth);
            lineWidth.SelectedIndexChanged+=delegate{if(updating)return;Doc.LineWidth=lineWidth.SelectedIndex+1;Doc.Dirty=Doc.Image!=null;RefreshUi();};
            numbers.CheckOnClick=true;numbers.Checked=true;tools.Items.Add(numbers);numbers.CheckedChanged+=delegate{if(updating)return;Doc.Numbers=numbers.Checked;Doc.Dirty=Doc.Image!=null;RefreshUi();};
            tools.Items.Add(new ToolStripSeparator());Button(tools,"−",delegate{Zoom(.8);});tools.Items.Add(zoomLabel);Button(tools,"＋",delegate{Zoom(1.25);});Button(tools,"Einpassen",delegate{Canvas.FitImage();});Button(tools,"100 %",delegate{Zoom(1/Canvas.Zoom);});
            var scaleTools=new ToolStrip {GripStyle=ToolStripGripStyle.Hidden,Padding=new Padding(10,4,10,4),BackColor=Color.FromArgb(239,247,244),Font=Font};
            calibrate=Button(scaleTools,"µm kalibrieren",delegate{Canvas.StartCalibration();});
            saveReference=Button(scaleTools,"Referenz speichern",delegate{SaveReference();});
            loadReference=Button(scaleTools,"Referenz laden",delegate{LoadReference();});
            reuseReference=Button(scaleTools,"Für weitere Bilder",delegate{References.Reuse=reuseReference.Checked;RefreshUi();});reuseReference.CheckOnClick=true;
            reuseReference.ToolTipText="Aktiv: gespeicherte Referenz für neu geöffnete Bilder mit gleicher Bildgröße übernehmen. Nur bei gleicher Vergrößerung und Skalierung verwenden.";
            removeCalibration=Button(scaleTools,"Entfernen",delegate{Cancel();Doc.SetCalibration(null);References.Reuse=false;RefreshUi();});removeCalibration.ToolTipText="Kalibrierung des aktuellen Bildes entfernen; Übernahme für weitere Bilder ausschalten.";
            scaleTools.Items.Add(new ToolStripSeparator());scaleTools.Items.Add(calibrationStatus);
            var seriesTools=new ToolStrip {GripStyle=ToolStripGripStyle.Hidden,Padding=new Padding(10,3,10,3),BackColor=Color.FromArgb(243,247,248),Font=Font};
            spikes=Button(seriesTools,"Stacheln messen",delegate{SetSpikeMode();});
            Button(seriesTools,"Bild hinzufügen",delegate{OpenImage();});
            Button(seriesTools,"Neue Messreihe",delegate{if(MayReplace())StartNewSeries();});
            seriesTools.Items.Add(new ToolStripSeparator());seriesTools.Items.Add(new ToolStripLabel("Bild:"));
            imagePicker.DropDownStyle=ComboBoxStyle.DropDownList;imagePicker.AutoSize=false;imagePicker.Width=270;seriesTools.Items.Add(imagePicker);
            imagePicker.SelectedIndexChanged+=delegate{if(!updating&&imagePicker.SelectedIndex>=0)SwitchImage(imagePicker.SelectedIndex);};
            guide.Dock=DockStyle.Top;guide.Height=48;guide.BackColor=Color.FromArgb(232,244,238);guide.ForeColor=Color.FromArgb(12,84,71);guide.Padding=new Padding(15,10,10,5);
            var body=new Panel {Dock=DockStyle.Fill};var side=new Panel {Dock=DockStyle.Right,Width=305,Padding=new Padding(16),BackColor=Color.White};
            Canvas=new MeasureCanvas(Doc);Canvas.Updated=RefreshUi;Canvas.ReferenceCompleted=CompleteCalibration;Viewport=new ImageViewport(Canvas);
            var work=new Panel {Dock=DockStyle.Fill};work.Controls.Add(Viewport);work.Controls.Add(guide);body.Controls.Add(work);body.Controls.Add(side);
            var sideLayout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=6};sideLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,34));sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,96));sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,34));sideLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,170));
            selection.Dock=DockStyle.Fill;selection.Font=new Font(Font,FontStyle.Bold);sideLayout.Controls.Add(selection,0,0);
            var fields=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=3};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,95));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            TextBox[] inputs={lengthBox,widthBox,angleBox};string[] labels={"Länge · px","Breite · px","Winkel · °"};string[] properties={"length","width","angle"};
            for(int i=0;i<3;i++){fields.RowStyles.Add(new RowStyle(SizeType.Percent,33.333f));fieldLabels[i]=new Label {Text=labels[i],Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};fields.Controls.Add(fieldLabels[i],0,i);inputs[i].Dock=DockStyle.Fill;inputs[i].Margin=new Padding(2,4,2,4);inputs[i].AccessibleName=labels[i];inputs[i].Tag=properties[i];inputs[i].Leave+=FieldLeave;inputs[i].KeyDown+=FieldKey;fields.Controls.Add(inputs[i],1,i);}
            sideLayout.Controls.Add(fields,0,1);ratio.Dock=DockStyle.Fill;ratio.Padding=new Padding(0,8,0,0);sideLayout.Controls.Add(ratio,0,2);
            count.Dock=DockStyle.Fill;count.Font=new Font(Font,FontStyle.Bold);
            measurementView.Dock=DockStyle.Fill;measurementView.DropDownStyle=ComboBoxStyle.DropDownList;measurementView.Items.AddRange(new object[]{"Sporen (0)","Stacheln (0)"});measurementView.SelectedIndex=0;measurementView.AccessibleName="Auswertung wählen: Sporen oder Stacheln";
            measurementView.SelectedIndexChanged+=delegate{if(updating)return;Cancel();Doc.Selected=0;RefreshUi();};sideLayout.Controls.Add(measurementView,0,3);
            table.Dock=DockStyle.Fill;table.ReadOnly=true;table.AllowUserToAddRows=false;table.AllowUserToDeleteRows=false;table.AllowUserToResizeRows=false;table.RowHeadersVisible=false;table.SelectionMode=DataGridViewSelectionMode.FullRowSelect;table.MultiSelect=false;table.BackgroundColor=Color.White;table.BorderStyle=BorderStyle.None;table.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
            table.EnableHeadersVisualStyles=false;table.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(239,245,246);table.DefaultCellStyle.SelectionBackColor=Color.FromArgb(217,239,230);table.DefaultCellStyle.SelectionForeColor=Color.FromArgb(20,60,50);table.RowTemplate.Height=30;table.AccessibleName="Sporen-Messungen";
            foreach(string col in new[]{"Nr.","L (px)","B (px)","Q"}){int n=table.Columns.Add(col,col);table.Columns[n].SortMode=DataGridViewColumnSortMode.NotSortable;table.Columns[n].FillWeight=n==0?60:100;}
            table.Columns.Add("Bild","Bild");table.Columns[4].FillWeight=55;table.Columns[4].SortMode=DataGridViewColumnSortMode.NotSortable;
            table.CellClick+=delegate(object sender,DataGridViewCellEventArgs e){if(updating||e.RowIndex<0)return;var row=(SeriesRow)table.Rows[e.RowIndex].Tag;SelectSpore(row.Spore.id);};
            sideLayout.Controls.Add(table,0,4);means.Dock=DockStyle.Fill;means.Padding=new Padding(0,12,0,0);means.ForeColor=Color.Blue;means.AccessibleName="Auswertung mit 95-Prozent-Mittelwertgrenzen";sideLayout.Controls.Add(means,0,5);
            side.Controls.Add(sideLayout);
            var bottom=new StatusStrip();status.Spring=true;status.TextAlign=ContentAlignment.MiddleLeft;bottom.Items.Add(status);bottom.Items.Add(new ToolStripStatusLabel("Mausrad: Zoom · Scrollbalken: Verschieben"));
            Controls.Add(body);Controls.Add(seriesTools);Controls.Add(scaleTools);Controls.Add(tools);Controls.Add(strip);Controls.Add(bottom);strip.Dock=DockStyle.Top;tools.Dock=DockStyle.Top;scaleTools.Dock=DockStyle.Top;seriesTools.Dock=DockStyle.Top;
            AllowDrop=true;DragEnter+=delegate(object sender,DragEventArgs e){if(e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect=DragDropEffects.Copy;};
            DragDrop+=delegate(object sender,DragEventArgs e){var paths=(string[])e.Data.GetData(DataFormats.FileDrop);if(paths!=null&&paths.Length>0){if(paths.Length==1&&paths[0].EndsWith(".json",StringComparison.OrdinalIgnoreCase))OpenPath(paths[0]);else foreach(var path in paths)try{AddImagePath(path);}catch(Exception ex){Error(ex);break;}}};
            FormClosing+=delegate(object sender,FormClosingEventArgs e){Cancel();if(!MayReplace())e.Cancel=true;};
            Deactivate+=delegate{Canvas.Space=false;if(Control.MouseButtons!=MouseButtons.None)Cancel();};
            KeyDown+=KeysDown;KeyUp+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Space)Canvas.Space=false;};
            FormClosed+=delegate{Series.Dispose();};
            RefreshUi();
        }
        private ToolStripButton Button(ToolStrip strip,string label,EventHandler click)
        {
            var b=new ToolStripButton(label) {DisplayStyle=ToolStripItemDisplayStyle.Text,Padding=new Padding(5,3,5,3)};b.Click+=delegate(object sender,EventArgs e){if(Canvas!=null)Canvas.Focus();click(sender,e);};strip.Items.Add(b);return b;
        }
        public void RefreshUi()
        {
            if(updating||IsDisposed)return;updating=true;
            try
            {
                Text=AppIdentity.WindowTitle;
                bool image=Doc.Image!=null;save.Enabled=image&&!Canvas.IsDraft;png.Enabled=image&&!Canvas.IsDraft;txt.Enabled=Series.Rows().Count>0&&!Canvas.IsDraft&&!Series.MixedUnits;undo.Enabled=Doc.Past.Count>0;redo.Enabled=Doc.Future.Count>0;delete.Enabled=Doc.Selection!=null;
                calibrate.Enabled=image;removeCalibration.Enabled=Doc.Calibration!=null;calibrate.Checked=Canvas.Calibrating;
                saveReference.Enabled=image&&Doc.Calibration!=null&&!Canvas.IsDraft;loadReference.Enabled=!Canvas.IsDraft;reuseReference.Enabled=References.Preset!=null;reuseReference.Checked=References.Reuse;
                calibrationStatus.Text=Doc.Calibration==null?"Noch kein Maßstab · Messwerte in px":"1 px = "+NumberDisplay.Detail(Doc.Scale,"0.########")+" µm · Referenz "+NumberDisplay.Detail(Doc.Calibration.micrometers,"0.######")+" µm";
                bool referenceMismatch=image&&Doc.Calibration==null&&References.Reuse&&References.Preset!=null&&!References.Preset.Matches(Doc);
                if(!image&&References.Reuse&&References.Preset!=null)calibrationStatus.Text="Referenz bereit · "+NumberDisplay.Detail(References.Preset.calibration.Scale(),"0.########")+" µm/px";
                if(referenceMismatch)calibrationStatus.Text="Andere Bildgröße · Referenz nicht übernommen";
                calibrationStatus.ToolTipText=References.Preset==null?calibrationStatus.Text:"Gespeicherte Referenz aus "+References.Preset.sourceImage+" · "+References.Preset.imageWidth+" × "+References.Preset.imageHeight+" px. Gleiche Bildgröße allein garantiert keinen gleichen Maßstab.";
                fieldLabels[0].Text="Länge · "+Doc.Unit;fieldLabels[1].Text="Breite · "+Doc.Unit;lengthBox.AccessibleName=fieldLabels[0].Text;widthBox.AccessibleName=fieldLabels[1].Text;
                if(Doc.Selection!=null&&(lastDocument!=Doc||lastSelected!=Doc.Selected))measurementView.SelectedIndex=Doc.Selection.isSpike?1:0;
                lastDocument=Doc;lastSelected=Doc.Selected;
                bool showSpikes=measurementView.SelectedIndex==1;
                var allRows=Series.Rows();measurementView.Items[0]="Sporen ("+allRows.Count(row=>!row.Spore.isSpike)+")";measurementView.Items[1]="Stacheln ("+allRows.Count(row=>row.Spore.isSpike)+")";
                var seriesRows=allRows.Where(row=>row.Spore.isSpike==showSpikes).ToList();string unit=seriesRows.Count==0?Doc.Unit:seriesRows[0].Document.Unit;
                table.Columns[1].HeaderText=Series.MixedUnits?"L":"L ("+unit+")";table.Columns[2].HeaderText=Series.MixedUnits?"B":"B ("+unit+")";
                table.Columns[2].Visible=table.Columns[3].Visible=!showSpikes;table.AccessibleName=showSpikes?"Stachel-Messungen":"Sporen-Messungen";
                table.Columns[4].Visible=Series.ImageCount>1;
                var imageLabels=Series.Documents.Select((d,i)=>(i+1)+" · "+(d.Image==null?"Kein Bild":d.Name)).ToArray();
                if(!imagePicker.Items.Cast<string>().SequenceEqual(imageLabels)){imagePicker.Items.Clear();imagePicker.Items.AddRange(imageLabels);}
                imagePicker.SelectedIndex=Series.ActiveIndex;
                draw.Checked=!Canvas.EditMode&&!Canvas.SpikeMode;spikes.Checked=!Canvas.EditMode&&Canvas.SpikeMode;edit.Checked=Canvas.EditMode;numbers.Checked=Doc.Numbers;lineWidth.SelectedIndex=Doc.LineWidth-1;zoomLabel.Text=image?Math.Round(Canvas.Zoom*100)+" %":"—";
                if(!image)guide.Text="1 · Über „Bild öffnen“ ein Mikroskopbild auswählen.";
                else if(Canvas.Calibrating||Canvas.ReferencePreview)guide.Text="Referenz: vom einen Ende des Maßstabsbalkens zum anderen ziehen und loslassen. Esc bricht ab.";
                else if(Canvas.DraftPhase=="width"){var d=Canvas.Draft();guide.Text="2 · Seitlich bewegen und Breite anklicken.  L "+Doc.MeasureText(d.length)+" "+Doc.Unit+" · W "+Doc.MeasureText(d.width)+" "+Doc.Unit;}
                else if(Canvas.DraftPhase=="axis")guide.Text=Canvas.SpikeMode?"Stachel: vom Ansatz zur Spitze ziehen und loslassen. Länge "+Doc.MeasureText(Canvas.Draft().length)+" "+Doc.Unit:"1 · Längsachse ziehen. Am anderen Sporenende loslassen.";
                else if(referenceMismatch)guide.Text="Bildgröße weicht von der Referenz ab. Neu kalibrieren oder mit „Referenz laden“ bewusst übernehmen.";
                else if(Canvas.SpikeMode&&!Canvas.EditMode)guide.Text="Stacheln messen · Vom Ansatz bis zur Spitze ziehen und loslassen. Endpunkte zum Korrigieren ziehen.";
                else guide.Text=Canvas.EditMode?"Rechteck wählen · Seitengriffe ziehen · runden Griff zum Drehen ziehen.":"1 · Längsachse ziehen → loslassen → 2 · seitlich bewegen und Breite anklicken";
                var r=Doc.Selection;selection.Text=r==null?(showSpikes?"Ausgewählter Stachel":"Ausgewählte Spore"):(r.isSpike?"Stachel ":"Spore ")+r.id;
                foreach(var box in new[]{lengthBox,widthBox,angleBox})box.Enabled=r!=null;
                if(r!=null)
                {
                    if(!lengthBox.Focused)lengthBox.Text=Doc.MeasureText(r.length);if(!widthBox.Focused)widthBox.Text=r.isSpike?"":Doc.MeasureText(r.width);widthBox.Enabled=!r.isSpike;if(!angleBox.Focused)angleBox.Text=NumberDisplay.Detail(Document.Normalize(r.angle)*180/Math.PI,"F1");
                    ratio.Text=r.isSpike?"Stachellänge · Ansatz bis Spitze":"Q = "+NumberDisplay.Value(r.length/r.width,2)+"   ·   L / B";
                }
                else{lengthBox.Text=widthBox.Text=angleBox.Text="";ratio.Text="Rechteck im Bild oder\nin der Tabelle auswählen.";}
                count.Text="Messungen · "+seriesRows.Count+(Series.ImageCount>1?" gesamt · "+Doc.Rects.Count+" im Bild":"");
                while(table.Rows.Count>seriesRows.Count)table.Rows.RemoveAt(table.Rows.Count-1);
                while(table.Rows.Count<seriesRows.Count)table.Rows.Add();
                for(int i=0;i<seriesRows.Count;i++)
                {
                    var entry=seriesRows[i];var r0=entry.Spore;var d0=entry.Document;var row=table.Rows[i];row.Tag=entry;
                    string suffix=Series.MixedUnits?" "+d0.Unit:"";
                    row.SetValues(r0.id,d0.RoundedMeasureText(r0.length)+suffix,r0.isSpike?"":d0.RoundedMeasureText(r0.width)+suffix,r0.isSpike?"":NumberDisplay.Value(r0.length/r0.width,2),entry.ImageIndex+1);
                    row.Selected=d0==Doc&&r0.id==Doc.Selected;row.Cells[4].ToolTipText=d0.Name;
                }
                UpdateSummary();
                status.Text=image?Doc.Name+" · "+Doc.Image.Width+" × "+Doc.Image.Height+" px"+(Series.Dirty?" · Ungespeichert":""):"Alles bleibt auf deinem Rechner. Das Originalbild bleibt unverändert.";
                Viewport.Sync();Canvas.Invalidate();
            }
            finally{updating=false;}
        }
        private void UpdateSummary()
        {
            string text;
            if(Series.MixedUnits)text=Series.MissingScaleMessage;
            else if(Series.Rows().Count==0)text=measurementView.SelectedIndex==1?Doc.SpikeSummaryText():Doc.SummaryText();
            else using(var aggregate=Series.Aggregate())text=measurementView.SelectedIndex==1?aggregate.SpikeSummaryText():aggregate.SummaryText();
            if(means.Text!=text)means.Text=text;
        }
        private void Cancel() { Canvas.Cancel(); }
        private void CompleteCalibration(PointF a,PointF b)
        {
            var cal=new CalibrationData {x1=a.X,y1=a.Y,x2=b.X,y2=b.Y};
            using(var dialog=new CalibrationDialog(cal.Pixels(),Doc.Calibration==null?10:Doc.Calibration.micrometers))
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                cal.micrometers=dialog.Micrometers;Doc.SetCalibration(cal);References.Reuse=false;RefreshUi();
            }
        }
        private void SaveReference()
        {
            if(Doc.Calibration==null||Doc.Image==null)return;
            using(var dialog=new SaveFileDialog {Title="Referenz für weitere Bilder speichern",Filter="Sporen-Referenz|*.sporen-referenz.json",DefaultExt="sporen-referenz.json",AddExtension=true,OverwritePrompt=true,FileName="Referenz-"+NumberDisplay.Detail(Doc.Calibration.micrometers,"0.######")+"um-"+Doc.Image.Width+"x"+Doc.Image.Height+".sporen-referenz.json"})
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                try{SaveReferenceFile(dialog.FileName);}catch(Exception ex){Error(ex);}
            }
        }
        public void SaveReferenceFile(string file)
        {
            var preset=CalibrationPreset.FromDocument(Doc);preset.Save(file);References.Use(preset);RefreshUi();
        }
        private void LoadReference()
        {
            using(var dialog=new OpenFileDialog {Title="Gespeicherte Referenz laden",Filter="Sporen-Referenz|*.sporen-referenz.json|JSON-Dateien|*.json"})
                if(dialog.ShowDialog(this)==DialogResult.OK)LoadReferencePath(dialog.FileName);
        }
        private void LoadReferencePath(string file)
        {
            try
            {
                var preset=CalibrationPreset.Load(file);
                if(Doc.Image!=null&&!preset.Matches(Doc))
                {
                    string message="Die Referenz stammt aus einem Bild mit "+preset.imageWidth+" × "+preset.imageHeight+" Pixeln.\nDas aktuelle Bild hat "+Doc.Image.Width+" × "+Doc.Image.Height+" Pixel.\n\nDer Maßstab passt nur, wenn Vergrößerung und Pixel-Skalierung gleich geblieben sind, etwa bei einem unvergrößerten Bildausschnitt.\n\nReferenz trotzdem für dieses Bild übernehmen?";
                    if(MessageBox.Show(this,message,"Abweichende Bildgröße",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
                }
                ApplyReferencePreset(preset);
            }
            catch(Exception ex){Error(ex);}
        }
        public void ApplyReferencePreset(CalibrationPreset preset)
        {
            CalibrationPreset.Validate(preset);Cancel();
            if(Doc.Image!=null)Doc.SetCalibration(preset.calibration);
            References.Use(preset);RefreshUi();
        }
        private void SetMode(bool editMode) { Cancel();Canvas.EditMode=editMode;if(!editMode){Canvas.SpikeMode=false;Doc.Selected=0;measurementView.SelectedIndex=0;}Canvas.Focus();RefreshUi(); }
        public void SetSpikeMode() { Cancel();Canvas.SpikeMode=true;Canvas.EditMode=false;Doc.Selected=0;measurementView.SelectedIndex=1;Canvas.Focus();RefreshUi(); }
        private void Zoom(double factor) { Canvas.ZoomBy(factor,new Point(Canvas.Width/2,Canvas.Height/2)); }
        private void FieldKey(object sender,KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Canvas.Focus();}
            if(e.KeyCode==Keys.Escape){var box=(TextBox)sender;var r=Doc.Selection;if(r!=null)box.Text=(string)box.Tag=="length"?Doc.MeasureText(r.length):(string)box.Tag=="width"?Doc.MeasureText(r.width):NumberDisplay.Detail(r.angle*180/Math.PI,"F2");box.Modified=false;e.SuppressKeyPress=true;Canvas.Focus();}
        }
        private void FieldLeave(object sender,EventArgs e)
        {
            if(updating)return;var box=(TextBox)sender;var r=Doc.Selection;if(r==null||!box.Modified)return;box.Modified=false;double val;
            bool ok=double.TryParse(box.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out val)||double.TryParse(box.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out val);
            string prop=(string)box.Tag;if(r.isSpike&&prop=="width")return;
            if(prop!="angle")val/=Doc.Scale;
            if(!ok||!Document.Finite(val)||(prop!="angle"&&val<.25)){MessageBox.Show(this,"Bitte einen gültigen Wert eingeben; Länge und Breite mindestens "+Doc.MeasureText(.25)+" "+Doc.Unit+".","Messwert",MessageBoxButtons.OK,MessageBoxIcon.Information);RefreshUi();return;}
            double previous=prop=="length"?r.length:prop=="width"?r.width:r.angle;if(prop=="angle")val=Document.Normalize(val*Math.PI/180);
            if(Math.Abs(previous-val)>0.000001){var before=Doc.Capture();if(prop=="length")r.length=val;else if(prop=="width")r.width=val;else r.angle=val;Doc.Commit(before);}RefreshUi();
        }
        private void KeysDown(object sender,KeyEventArgs e)
        {
            if(e.Control&&e.KeyCode==Keys.S){e.SuppressKeyPress=true;Canvas.Focus();SaveProject(e.Shift);return;}
            if(ActiveControl is TextBox)return;
            if(e.Control&&e.KeyCode==Keys.Z){Cancel();if(e.Shift)Doc.Redo();else Doc.Undo();e.SuppressKeyPress=true;}
            else if(e.Control&&e.KeyCode==Keys.Y){Cancel();Doc.Redo();e.SuppressKeyPress=true;}
            else if(e.KeyCode==Keys.Delete||e.KeyCode==Keys.Back){Cancel();Doc.Delete();e.SuppressKeyPress=true;}
            else if(e.KeyCode==Keys.Escape){Cancel();Doc.Selected=0;e.SuppressKeyPress=true;}
            else if(e.KeyCode==Keys.Space){Canvas.Space=true;e.SuppressKeyPress=true;}
            else if(!e.Control&&e.KeyCode==Keys.N)SetMode(false);
            else if(!e.Control&&e.KeyCode==Keys.V)SetMode(true);
            RefreshUi();
        }
        private bool MayReplace()
        {
            if(!Series.Dirty)return true;var answer=MessageBox.Show(this,"Die Messreihe ist noch nicht gespeichert. Jetzt speichern?","Messung speichern",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
            if(answer==DialogResult.Cancel)return false;if(answer==DialogResult.No)return true;return SaveProject(false);
        }
        private void Error(Exception ex) { MessageBox.Show(this,"Das hat nicht geklappt.\n\n"+ex.Message,"Sporenmessung",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        private void OpenImage()
        {
            using(var dlg=new OpenFileDialog {Title="Bilder zur Messreihe hinzufügen",Multiselect=true,Filter="Mikroskopbilder|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|Alle Dateien|*.*"})
                if(dlg.ShowDialog(this)==DialogResult.OK)foreach(var path in dlg.FileNames)try{AddImagePath(path);}catch(Exception ex){Error(ex);break;}
        }
        private void StoreView() {if(Doc.Image!=null)views[Doc]=new[]{Canvas.Zoom,Canvas.Ox,Canvas.Oy,(double)Canvas.Width,(double)Canvas.Height};}
        private void ShowCurrent()
        {
            Canvas.Doc=Doc;double[] view;
            if(views.TryGetValue(Doc,out view)){Canvas.Zoom=view[0];Canvas.Ox=view[1]+(Canvas.Width-view[3])/2;Canvas.Oy=view[2]+(Canvas.Height-view[4])/2;}
            else Canvas.FitImage();
            Canvas.Focus();RefreshUi();
        }
        public void SwitchImage(int index)
        {
            if(index<0||index>=Series.Documents.Count||index==Series.ActiveIndex)return;
            Cancel();StoreView();Series.ActiveIndex=index;ShowCurrent();
        }
        public void SelectSpore(int id)
        {
            var row=Series.Rows().Find(r=>r.Spore.id==id);if(row==null)return;
            Cancel();SwitchImage(row.ImageIndex);Doc.Selected=id;RefreshUi();
        }
        public void AddImagePath(string path)
        {
            Cancel();StoreView();Series.AddImage(path,References);ShowCurrent();
        }
        public void StartNewSeries()
        {
            Cancel();Series.Clear();views.Clear();projectPath="";Canvas.Doc=Doc;Canvas.SpikeMode=false;Canvas.EditMode=false;measurementView.SelectedIndex=0;RefreshUi();
        }
        public void SaveSeriesFile(string file) {Series.Save(file);projectPath=file;RefreshUi();}
        private void OpenProject()
        {
            using(var dlg=new OpenFileDialog {Title="Messung öffnen",Filter="Sporen-Messdateien|*.sporen.json;*.json"})if(dlg.ShowDialog(this)==DialogResult.OK)OpenPath(dlg.FileName);
        }
        public void OpenPath(string path)
        {
            if(path.EndsWith(".sporen-referenz.json",StringComparison.OrdinalIgnoreCase)){LoadReferencePath(path);return;}
            if(!MayReplace())return;try{Cancel();Series.Load(path,References);views.Clear();projectPath=path.EndsWith(".json",StringComparison.OrdinalIgnoreCase)?Path.GetFullPath(path):"";ShowCurrent();}catch(Exception ex){Error(ex);}
        }
        private string BaseName() { return Path.GetFileNameWithoutExtension(Doc.Name); }
        private bool SaveProject(bool saveAs)
        {
            if(Doc.Image==null)return false;if(Canvas.IsDraft){MessageBox.Show(this,"Bitte die Breite festlegen oder die begonnene Messung mit Esc abbrechen.","Messung abschließen");return false;}
            string file=saveAs?"":projectPath;
            if(file=="")using(var dlg=new SaveFileDialog {Title="Messung speichern",Filter="Sporen-Messdatei|*.sporen.json",FileName=BaseName()+".sporen.json",DefaultExt="sporen.json",AddExtension=true,OverwritePrompt=true}){if(dlg.ShowDialog(this)!=DialogResult.OK)return false;file=dlg.FileName;}
            try{SaveSeriesFile(file);return true;}catch(Exception ex){Error(ex);return false;}
        }
        private void Export(bool asText)
        {
            if(Doc.Image==null||Canvas.IsDraft)return;
            using(var dlg=new SaveFileDialog {Title=asText?"Messwerte als TXT oder CSV speichern":"Markierte Bildkopie speichern",Filter=asText?"Textdatei (*.txt)|*.txt|CSV-Datei (*.csv)|*.csv":"JPG-Bild (*.jpg; *.jpeg)|*.jpg;*.jpeg|PNG-Bild (*.png)|*.png",FileName=BaseName()+(asText?"-messwerte":"-markiert"),AddExtension=true,OverwritePrompt=true})
            {
                if(dlg.ShowDialog(this)!=DialogResult.OK)return;
                try
                {
                    if(asText)Series.Export(dlg.FileName);
                    else
                    {
                        string ext=Path.GetExtension(dlg.FileName).ToLowerInvariant();
                        if(ext==".jpg"||ext==".jpeg")Doc.Jpg(dlg.FileName);
                        else if(ext==".png")Doc.Png(dlg.FileName);
                        else throw new InvalidOperationException("Bitte eine JPG- oder PNG-Dateiendung verwenden.");
                    }
                }
                catch(Exception ex){Error(ex);}
            }
        }
        private void ShowOptions()
        {
            Canvas.Focus();
            using(var dialog=new OptionsDialog(NumberDisplay.Current))
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                try { var options=dialog.Options;options.Save(NumberOptions.FilePath);NumberDisplay.Current=options;RefreshUi(); }
                catch(Exception ex) { Error(ex); }
            }
        }
        private void Help()
        {
            MessageBox.Show(this,"Stacheln messen: Die Schaltfläche wählen, vom Ansatz bis zur Spitze ziehen und loslassen. Zwei Endpunkte dienen zum Korrigieren, Ziehen an der Linie verschiebt sie. Rechts zwischen Sporen und Stacheln wechseln. Stacheln haben eine eigene Längenauswertung, ohne Breite, Q oder Volumen. TXT und CSV enthalten einen getrennten Abschnitt. Messdateien mit Stacheln benötigen diese neue Version. Gemessen wird die gerade Strecke im Bild.\n\nMessreihe: „Bild öffnen“ und „Bild hinzufügen“ behalten alle bisherigen Messungen. Mehrere Fotos können gleichzeitig ausgewählt werden. Über die Bildauswahl oder eine Tabellenzeile wechselst du zurück. Jedes Bild behält seinen Maßstab. Die Tabelle, Auswertung und TXT/CSV-Exporte enthalten alle Sporen. Bei gemischten px/µm erst die fehlenden Bilder kalibrieren. „Messung speichern“ sichert die ganze Reihe einschließlich aller Bilder. „Neue Messreihe“ beginnt neu. Bildkopie speichern exportiert nur das aktuelle Bild.\n\nMessen: Längsachse ziehen, loslassen, seitlich bewegen und Breite anklicken. Sofort mit der nächsten Spore weitermachen.\n\nKorrigieren: Rechteck auswählen. Seitengriffe ändern die Größe; der grüne Punkt dreht. Ziehen im Inneren verschiebt.\n\nN: Messen · V: Korrigieren · Entf: Löschen\nEsc: Abbrechen · Strg+Z: Rückgängig · Strg+Y: Wiederholen\nStrg+S: Speichern · Strg+Shift+S: Speichern unter\nMausrad: Zoom · Leertaste + Ziehen: Bild verschieben\nScrollbalken oben: links/rechts · rechts: hoch/runter. Nach „Einpassen“ sind die Balken deaktiviert.\nShift + Ziehen: neue Messung über einem vorhandenen Rechteck\n\nGelöschte Nummern werden wieder verwendet.\nµm kalibrieren: über den Maßstabsbalken ziehen und seine bekannte Länge in µm eingeben. Auch vorhandene Rechtecke werden umgerechnet; Q bleibt gleich. Die Kalibrierung gilt für dieses Bild und wird in der Messdatei mitgespeichert. „Referenz speichern“ sichert den Maßstab separat. „Referenz laden“ übernimmt ihn auch für Bilder ohne Balken. „Für weitere Bilder“ übernimmt die gespeicherte Referenz für neu geöffnete Bilder gleicher Größe. Das gilt nur bei gleichen Aufnahme- und Skalierungseinstellungen. Gespeicherte Messungen behalten immer ihre eigene Kalibrierung. Nach einem Programmneustart die Referenz einmal laden. Ohne Kalibrierung werden Pixel angezeigt.\n\nMessung speichern sichert Bild und bearbeitbare Rechtecke. Auch die bisherigen .sporen.json-Dateien aus der Browser-Version lassen sich öffnen (PNG, JPG, BMP). Bildkopie speichern bietet JPG (Qualität 95 %) und PNG in Originalauflösung. Das Original bleibt unverändert.\n\nMesswerte speichern bietet TXT und CSV im Dateityp-Feld. Beide exportieren Länge, Breite, Q und geschätztes Volumen sowie Minimum, Maximum und Mittelwert wie in deiner Vorlage. Mit Kalibrierung in µm bzw. µm³, sonst in px bzw. px³. Das Volumen wird als Rotationsellipsoid berechnet: V = π/6 × L × B², mit Tiefe = Breite. CSV erläutert die Berechnung. TXT enthält am Ende zusätzlich eine kompakte Zusammenfassung für L, B und Q mit 95-%-Mittelwertgrenzen. Unter fünf Sporen werden dort nur Mittelwerte angegeben. Unter „Optionen“ sind Komma oder Punkt und 0, 1 oder 2 Nachkommastellen wählbar, getrennt für L/B, Q und Volumen. Die Auswahl wird gespeichert und gilt für Anzeige, TXT und CSV. „Maße am Bild anzeigen“ blendet Länge und Breite außen an den Rechteckseiten ein, auch in JPG-/PNG-Bildkopien. Ohne Kalibrierung in px, mit Kalibrierung in µm. Die Schaltfläche „Nummern“ bleibt davon unabhängig. Die Maßbeschriftungen lassen sich einzeln mit der linken Maustaste ziehen, ohne die Messwerte zu ändern. Strg+Z macht das Verschieben rückgängig. „Messung speichern“ sichert die Positionen; JPG/PNG übernimmt sie. Die internen Messwerte bleiben ungerundet. CSV verwendet weiterhin Semikolons als Spaltentrenner. Die blaue Auswertung zeigt untere Grenze – fettgedruckten Mittelwert – obere Grenze (95 %) für L, B und Q. CSV enthält zusätzlich MW unten/oben (95 %) sowie PG unten/oben (95 %) für Länge, Breite, Q und Volumen, ab fünf Sporen. PG verwendet die Näherung nach Gerd Fischer (Mittelwert ± t × s), MW verwendet Mittelwert ± t × s / √n. Darunter steht n.a. Die Grenzen werden nach der Student-t-Verteilung aus ungerundeten Werten berechnet und nach außen auf die eingestellten Nachkommastellen gerundet.","Bedienung",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
    }
    internal static class Program
    {
        [STAThread] private static void Main(string[] args)
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            System.Threading.Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");
            Application.ThreadException+=delegate(object sender,System.Threading.ThreadExceptionEventArgs e){MessageBox.Show("Ein Fehler ist aufgetreten: "+e.Exception.Message,"Sporenmessung");};
            try { NumberDisplay.Current=NumberOptions.Load(NumberOptions.FilePath); }
            catch(Exception) { MessageBox.Show("Die gespeicherten Optionen konnten nicht geladen werden. Es gelten vorerst Komma und zwei Nachkommastellen. Du kannst die Auswahl unter Optionen erneut speichern.","Optionen",MessageBoxButtons.OK,MessageBoxIcon.Information); }
            using(var form=new MainForm()){if(args.Length>0)form.Shown+=delegate{form.OpenPath(args[0]);};Application.Run(form);}
        }
    }
}
