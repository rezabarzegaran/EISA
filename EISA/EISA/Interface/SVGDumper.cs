using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void SaveFlow(Result result)
        {
            try
            {
                Duration = result.Hyperperiod * mlt;
                maxH = result.getNOutPorts();
                DocumentBuilderFactory dbFactory = DocumentBuilderFactory.newInstance();
                DocumentBuilder dBuilder = dbFactory.newDocumentBuilder();
                Document doc = dBuilder.newDocument();

                Element svg = init(doc, solution);
                doc.appendChild(svg);

                for (Stream stream : solution.streams)
                {
                    for (String node : stream.routingList)
                    {
                        Port crr_Port = getPortObject(solution, node, stream.Id);
                        int s_index = getStreamIndex(solution, node, stream.Id);
                        int port_index = getPortIndex(solution, node, stream.Id);
                        for (int i = 0; i < crr_Port.indexMap[s_index].length; i++)
                        {
                            int x_1 = crr_Port.Topen[crr_Port.indexMap[s_index][i]] * mlt;
                            int x_2 = crr_Port.Tclose[crr_Port.indexMap[s_index][i]] * mlt;
                            int width = x_2 - x_1;
                            if ((x_1 <= Duration) && (x_2 <= Duration))
                            {
                                addFrame(doc, svg, x_1 + offset, rowH * port_index + Toffset, width, rowH, String.valueOf(stream.Id), stream.Priority);
                            }

                        }
                    }
                }

                TransformerFactory transformerFactory = TransformerFactory.newInstance();
                Transformer transformer = transformerFactory.newTransformer();
                DOMSource domSource = new DOMSource(doc);
                Files.createDirectories(Paths.get(DirPath));
                String path = DirPath + "/" + "scheduletable.svg";
                StreamResult streamResult = new StreamResult(new File(path));
                transformer.transform(domSource, streamResult);


            }
            catch (Exception e)
            {
                e.printStackTrace();
            }

        }
    }
}
