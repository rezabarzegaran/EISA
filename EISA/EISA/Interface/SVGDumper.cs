using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using EISA.Base;

namespace EISA.Interface
{
    public class SVGDumper
    {
        private int Duration = 0;
        private int offset = 220;
        private int Toffset = 30;
        private int rowH = 30;
        private int maxH = 0;
        private int mlt = 1;
        private int co = 1;
        public SVGDumper()
        {

        }



        public void Save(Result result, string filename)
        {
            Duration = result.Hyperperiod * mlt;
            maxH = result.Links.Count + result.Cores.Count;
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter xmlWriter = XmlWriter.Create(filename, settings);
            //XmlWriter xmlWriter = XmlWriter.Create(filename);


            xmlWriter.WriteStartDocument();
            initSvGFile(xmlWriter, result);

            foreach (Result.Stream s in result.Streams)
            {
                foreach (var Link in s.getLinks())
                {
                    int port_index = result.Links.FindIndex(x => x.Equals(Link.getName()));

                    if (port_index != -1)
                    {
                        foreach (var Frame in Link.getFrames())
                        {
                            DrawFrame(xmlWriter, Frame.getOpen() + offset, rowH * port_index + Toffset, (Frame.getClose() - Frame.getOpen()), rowH, s.Name, s.Priority);
                        }
                    }

                }
            }

            foreach (Result.VTask _task in result.Tasks)
            {
                int port_index = result.Cores.FindIndex(x => x.Equals(_task.getMap()));

                if (port_index != -1)
                {
                    port_index += result.Links.Count;
                    foreach (Result.VTask.Job Job in _task.getJobs())
                    {
                        DrawFrame(xmlWriter,  Job.getStart()+ offset, rowH * port_index + Toffset, (Job.getEnd() - Job.getStart()), rowH, _task.Name, 0);

                    }

                }
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();

        }

        private void initSvGFile(XmlWriter writer, Result result)
        {
            
            writer.WriteStartElement("svg", "http://www.w3.org/2000/svg");
            //writer.WriteStartElement("svg", "xmlns", "http://www.w3.org/2000/svg");

            //writer.WriteAttributeString("xmlns", "http://www.w3.org/2000/svg");
            writer.WriteAttributeString("xmlns", "ev" , null , "http://www.w3.org/2001/xml-events");
            writer.WriteAttributeString("xmlns",  "xlink", null, "http://www.w3.org/1999/xlink");
            writer.WriteAttributeString("baseProfile", "full");
            string _height = (rowH * (maxH + 1) + Toffset).ToString();
            writer.WriteAttributeString("height", _height);
            string _width = (Duration + 2 * offset).ToString();
            writer.WriteAttributeString("width", _width);

            //writer.WriteStartElement("defs");
            DrawLine(writer, (offset + 0), ((rowH * maxH) + Toffset), (offset + Duration), ((rowH * maxH) + Toffset), "bottum");

            for (int i = 0; i <= (Duration / 100); i++)
            {
                DrawLine(writer, (offset + (i * 100)), (0 + Toffset), (offset + (i * 100)), ((rowH * maxH) + Toffset), "mainvertical");
            }

            for (int i = 0; i <= (Duration / 10); i++)
            {
                if ((i % 100) != 0)
                {
                    DrawLine(writer, (offset + (i * 10)), (0 + Toffset), (offset + (i * 10)), ((rowH * maxH) + Toffset), "subvertical");

                }
            }

            for (int i = 0; i <= (Duration / 100); i++)
            {
                DrawText(writer, (i * 100 * co).ToString(), (offset + (i * 100)), ((rowH * (maxH + 1)) + Toffset), "timeTag");
            }

            for (int i = 0; i < (result.Links.Count + result.Cores.Count); i++)
            {
                DrawLine(writer, 0, i * rowH + Toffset, offset + Duration, i * rowH + Toffset, "divider");

            }

            //List<String> portListaStrings = solution.getOutPorts();
            for (int i = 0; i < result.Links.Count; i++)
            {
                DrawText(writer, result.Links[i], offset / 2, (rowH * i) + (2 * rowH / 3) + Toffset, "portTag");

            }

            for (int i = 0; i < result.Cores.Count; i++)
            {
                int j = i + result.Links.Count;
                DrawText(writer, result.Cores[i], offset / 2, (rowH * j) + (2 * rowH / 3) + Toffset, "portTag");

            }

            //writer.WriteEndElement();




        }

        private void DrawLine(XmlWriter writer, int x1, int y1, int x2, int y2, String status)
        {
            writer.WriteStartElement("line");
            writer.WriteAttributeString("x1", x1.ToString());
            writer.WriteAttributeString("y1", y1.ToString());
            writer.WriteAttributeString("x2", x2.ToString());
            writer.WriteAttributeString("y2", y2.ToString());
            switch (status)
            {
                case "bottum":
                    writer.WriteAttributeString("style", "stroke:red ;stroke-width:3");
                    break;
                case "mainvertical":
                    writer.WriteAttributeString("style", "stroke:gray ;stroke-width:0.5");
                    break;

                case "subvertical":
                    writer.WriteAttributeString("style", "stroke:silver ;stroke-width:0.3");
                    break;
                case "divider":
                    writer.WriteAttributeString("style", "stroke:silver ;stroke-width:1");
                    break;

                default:
                    break;

            }
            writer.WriteEndElement();
        }

        private void DrawText(XmlWriter writer, string txt, int x, int y, string where)
        {
            writer.WriteStartElement("text");
            writer.WriteAttributeString("x", x.ToString());
            writer.WriteAttributeString("y", y.ToString());
            writer.WriteAttributeString("text-anchor", "middle");

            switch (where)
            {
                case "timeTag":
                    writer.WriteAttributeString("fill", "black");
                    writer.WriteAttributeString("style", "font-style:bold ; font-family: times; font-size: 24px");
                    break;
                case "portTag":
                    writer.WriteAttributeString("fill", "black");
                    writer.WriteAttributeString("style", "font-style:italic; font-family: times; font-size: 20px");
                    break;
                case "frameTag":
                    writer.WriteAttributeString("fill", "black");
                    writer.WriteAttributeString("style", "font-style:bold font-family: times; font-size: 20px");
                    break;

                default:
                    break;
            }
            writer.WriteString(txt);
            writer.WriteEndElement();

        }

        private void DrawFrame(XmlWriter writer, int x, int y, int width, int height, String txt, int pri)
        {
            string colorString = null;
            switch (pri)
            {
                case 0:
                    colorString = "maroon";
                    break;
                case 1:
                    colorString = "olive";
                    break;
                case 2:
                    colorString = "red";
                    break;
                case 3:
                    colorString = "yellowgreen";
                    break;
                case 4:
                    colorString = "cyan";
                    break;
                case 5:
                    colorString = "royalblue";
                    break;
                case 6:
                    colorString = "goldenrod";
                    break;
                case 7:
                    colorString = "yellow";
                    break;

                default:
                    break;
            }

            Drawbox(writer, x, y, width, height, colorString);
            DrawText(writer, txt, x + (width / 2), y + (2 * rowH / 3), "frameTag");
        }

        private void Drawbox(XmlWriter writer, int x, int y, int width, int height, String color)
        {
            String styleString = "fill:" + color + ";stroke:black;stroke-width:1;opacity:0.75";

            writer.WriteStartElement("rect");
            writer.WriteAttributeString("x", x.ToString());
            writer.WriteAttributeString("y", y.ToString());
            writer.WriteAttributeString("width", width.ToString());
            writer.WriteAttributeString("height", height.ToString());
            writer.WriteAttributeString("rx", "3");
            writer.WriteAttributeString("ry", "3");
            writer.WriteAttributeString("style", styleString);
            writer.WriteEndElement();


        }
    }
}
