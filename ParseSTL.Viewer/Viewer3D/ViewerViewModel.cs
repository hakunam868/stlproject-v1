//-----------------------------------------------------------------------------------------------!
// Copyright (c) 2021 MAKER AI (info@mak3r.ai) All rights reserved.
// This file is owned by MAKER AI Private Limited (www.mak3r.ai)
// This source file is the confidential property of MAKER AI.
// Reproduction or transmission in whole or in part, in any form or
// by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//-------------------------------------------------------------------------------------------------!

using Microsoft.JSInterop;
using System.Diagnostics;
using System.Reflection;

namespace ParseSTL.Viewer.Viewer3D;

public class ViewerViewModel
{
    private int fileCounter = 1;

    public int transmission = 10;
    [JSInvokable]
    public void saveCapture(byte[] imageBuffer)
    {
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string fileName = $"canvas_image_{fileCounter}.png";
        string filePath = Path.Combine(folderPath, fileName);
        if (imageBuffer != null && imageBuffer.Length > 0)
        {
            // The canvas already hands over PNG bytes (toBlob default), so write them as-is.
            // Maker re-encodes through ImageSharp; the file comes out the same without the package.
            File.WriteAllBytes(filePath, imageBuffer);
            fileCounter++; // Increment file counter for the next file

        }
    }
    [JSInvokable]
    public byte[]? loadFile(string filePath)
    {
        byte[]? stlBuffer = null;
        string filename = filePath;

        using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fileStream))
        {
            long fileSize = new FileInfo(filename).Length;
            var sw = new Stopwatch();
            sw.Start();
            stlBuffer = reader.ReadBytes((int)fileSize);
            sw.Stop();
            Console.WriteLine("Time for reading buffer: " + sw.Elapsed.Seconds);
        }
        return stlBuffer;
    }
    [JSInvokable]
    public Byte[] jsFetch(string filename)
    {
        filename = filename.Split('?', '#')[0].Split('/', '\\').Last();
        string embeddedFileName = "occt-import-js.wasm";
        string embeddedResourceName = "ParseSTL.Viewer.Viewer3D." + embeddedFileName;
        var assembly = Assembly.GetExecutingAssembly();

        // Check if the requested file is the embedded file
        if (filename == embeddedFileName)
        {
            using (Stream? stream = assembly.GetManifestResourceStream(embeddedResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("The embedded resource was not found.", embeddedResourceName);
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        // If the file is not embedded, proceed with the file system path logic
        var result1 = Assembly.GetExecutingAssembly().Location;
        var result2 = Path.GetDirectoryName(result1)!.Replace('\\', '/');
        var path = result2.Replace(@"file:/", string.Empty);
        filename = $"{path}/Viewer3D/{filename}";
        Console.WriteLine("Received fetch request for file:" + filename);

        return File.ReadAllBytes(filename);
    }
}
