using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace GenshinWishOcr
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 1)
            {
                string folder = args[0];
                string fileName = args[1]; 
                List<Selection> innerSelection = new List<Selection>();
                List<MyPage> pages = new List<MyPage>();
                int x = 0;
                string[] files = Directory.GetFiles(folder);
                List<string> fileList = files.ToList();
                fileList.Sort();
                List<Task> tasks = new List<Task>();
                int threadCount = 1;
                if(args.Length > 2)
                {
                    threadCount = Convert.ToInt32(Environment.ProcessorCount * Double.Parse(args[2]));
                }
                Console.WriteLine($"Running ocr job with threads: {threadCount}.");
                BlockingCollection<int> progessTracker = new BlockingCollection<int>();
                ConcurrentDictionary<string, MyPage> concurrentPageMap = new ConcurrentDictionary<string, MyPage>();
                int amt = fileList.Count() / threadCount;
                int startIndex = 0;
                for (int i = 0; i < threadCount; i++)
                {
                    int endIndex = startIndex + amt;
                    if(i == threadCount - 1)
                    {
                        endIndex = fileList.Count();
                    }
                    tasks.Add(OcrPagesTask(fileList, startIndex, endIndex, concurrentPageMap, progessTracker));
                    startIndex = endIndex;
                }
                CancellationTokenSource cts = new CancellationTokenSource();
                var progressTask = Task.Run(() =>
                {
                    int progress = 0;
                    int fileCount = fileList.Count;
                    foreach (int x in progessTracker.GetConsumingEnumerable())
                    {
                        progress += x;
                        double d1 = (double)progress / (double)fileCount;
                        Console.Write($"Progress {d1:P1}");
                        Console.SetCursorPosition(0, Console.CursorTop);
                        if(progress >= fileCount)
                        {
                            progessTracker.CompleteAdding();
                        }
                    }
                }, cts.Token);
                //await Task.WhenAll(tasks);
                foreach(var task in tasks)
                {
                    await task; // toss exception, can we use Task.WhenAll()?
                }

                await Task.Delay(2000);
                cts.Cancel();
                await Task.Delay(2000);
                Console.WriteLine();

                int c = concurrentPageMap.Count();
                Console.WriteLine($"{c} pages in {fileList.Count} files ocr'd.");

                for (int z = 0; z < fileList.Count; z++)
                {
                   
                    string file = fileList[z];
                    
                    MyPage page;
                    if (concurrentPageMap.TryGetValue(file, out page))
                    {
                        if (page.Selections.Count > 0)
                        {
                            if (x == 0)
                            {
                                // first page auto add;
                                page.Index = x;
                                pages.Add(page);
                                x++;
                            }
                            else
                            {
                                if (pages[x - 1].Equals(page))
                                {
                                    // skip dupe page
                                }
                                else
                                {
                                    page.Index = x;
                                    pages.Add(page);
                                    x++;
                                }
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                Console.WriteLine("Complete. Writing to file now");
                int y = 1;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Roll,Four Pity,Five Pity,Type,Item,Date");
                int fourPity = 1;
                int fivePity = 1;
                DateTime date = DateTime.MinValue;
                int c1 = 1;
                foreach (var page in pages.OrderByDescending(x => x.Index))
                {
                    foreach (var sel in page.Selections.OrderByDescending(x => x.Index))
                    {
                        sb.AppendLine($"{y},{fourPity},{fivePity},{sel.Type},{sel.Item},{sel.Date:yyyy-MM-dd HH:mm:ss}");
                        if (sel.Date > date)
                        {
                            date = sel.Date;
                            c1 = 1;
                        }
                        else if(sel.Date < date)
                        {
                            c++;
                            Console.WriteLine($"WARNING: Duplication detected on line: {y}");
                        }
                        else
                        {
                            c1++;
                            if (c1 > 10)
                            {
                                Console.WriteLine($"WARNING: Duplication detected on line: {y}");
                                c1 = 1;
                            }
                        }
                        //Console.WriteLine($"WARNING: ");
                        y++;
                        fourPity++;
                        fivePity++;
                        if (sel.Item.Contains("5-Star", StringComparison.InvariantCultureIgnoreCase))
                        {
                            fivePity = 1;
                        }
                        else if(sel.Item.Contains("4-Star", StringComparison.InvariantCultureIgnoreCase))
                        {
                            fourPity = 1;
                        }
                    }
                }
                File.WriteAllText(fileName, sb.ToString());
                Console.WriteLine("Finish Ocr Job.");
            }
        }

        private static Task OcrPagesTask(List<string> fileList, int startIndex, int endIndex, ConcurrentDictionary<string, MyPage> pageMap, BlockingCollection<int> tracker)
        {
            return Task.Run(() =>
            {
                int x = 0;
                for(int i = startIndex; i < endIndex; i++)
                {
                    string file = fileList[i];
                    MyPage page;
                    if(OcrPage(file, out page))
                    {
                        pageMap.TryAdd(file, page);
                    }
                    x++;
                    if (x % 10 == 0)
                    {
                        tracker.Add(x);
                        x = 0;
                    }
                }
                tracker.Add(x);
            });
        }

        public static bool OcrPage(string file, out MyPage page)
        {
            page = new MyPage();
            using (var ocrengine = new TesseractEngine(@".\tessdata", "eng", EngineMode.Default))
            {
                var img = Pix.LoadFromFile(file);
                //img = img.ConvertRGBToGray();
                var res = ocrengine.Process(img);
                var iter = res.GetIterator();
                // parse header

                bool headerFound = false;
                do
                {
                    string header = iter.GetText(PageIteratorLevel.TextLine).Trim();
                    if (header.Contains("Time Received"))
                    {
                        headerFound = true;
                    }

                } while (!headerFound && iter.Next(PageIteratorLevel.TextLine));

                if (headerFound)
                {
                    int x = 0;
                    while (iter.Next(PageIteratorLevel.TextLine))
                    {
                        string s = iter.GetText(PageIteratorLevel.TextLine);
                        s = s.Trim();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            string[] lines = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length > 3)
                            {
                                string typeLine = lines[0].ToLower();
                                bool isWeapon = typeLine.Contains("weapon");
                                bool isCharacter = typeLine.Contains("character");
                                if (!isWeapon && !isCharacter)
                                {
                                    Console.WriteLine($"Faulty type found in file {file}, line {s}.");
                                    return false;
                                }
                                else if (isWeapon && isCharacter)
                                {
                                    throw new Exception("this should never happen");
                                }

                                string dateStr = $"{lines[lines.Length - 2]} {lines[lines.Length - 1]}";
                                DateTime date;
                                if (!DateTime.TryParse(dateStr, out date))
                                {
                                    Console.WriteLine($"Incorrect date format, file {file}, line {s}.");
                                    return false;
                                }
                                StringBuilder sb = new StringBuilder();
                                bool first = true;
                                for (int i = 1; i < lines.Length - 2; i++)
                                {
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        sb.Append(" ");
                                    }
                                    sb.Append(lines[i]);
                                }
                                string valueItem = sb.ToString();
                                valueItem = Massage(valueItem);
                                Selection sel = new Selection(x, isWeapon, isCharacter, valueItem, date);
                                x++;
                                page.Selections.Add(sel);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error: no header found for {file}, Data: {res.GetText().Trim()}.");
                    return false;
                }
                //Console.WriteLine(res.GetText().Trim());
                if (page.Selections.Count > 6)
                {
                    Console.WriteLine($"Error: too many selections for {file}, Data: {res.GetText().Trim()}.");
                }
            }
            return true;
        }

        private static Dictionary<string, string> _massageMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Keging (5-Star)", "Keqing (5-Star)" },
            { "Qigi (5-Star)", "Qiqi (5-Star)" },
            { "Xinggiu (4-Star)", "Xingqiu (4-Star)" },
            { "Fisch (4-Star)", "Fischl (4-Star)" },
        };
        private static string Massage(string item)
        {

            item = item.Replace("`", "").Replace("‘", "");
            string outItem;
            if(_massageMap.TryGetValue(item, out outItem))
            {
                return outItem;
            }
            return item;
        }
    }
}
