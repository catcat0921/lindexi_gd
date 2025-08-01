﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeleehayherfojalkemWireawakea;

internal partial class ImageProvider
{
    public required DirectoryInfo OriginFolder { get; init; }

    public required ImageManager ImageManager { get; init; }

    public required CnBlogsImageUploader CnBlogsImageUploader { get; init; }

    [GeneratedRegex(@"<!--\s*!\[\]\(image/([\w /\.]*)\)\s-->")]
    private static partial Regex GetImageFileRegex();

    [GeneratedRegex(@"!\[\]\(http://cdn.lindexi.site/")]
    private static partial Regex GetImageLinkRegex();

    [GeneratedRegex(@"!\[\]\(http://image.acmx.xyz/")]
    private static partial Regex GetImageLinkRegex2();

    [GeneratedRegex(@"<!--\s*CreateTime:([\d/\s:]*)\s*-->")]
    private static partial Regex GetCreateTimeRegex();

    public void Convert(FileInfo blogFile)
    {
        var imageFileRegex = GetImageFileRegex();
        var imageLinkRegex = GetImageLinkRegex();
        var imageLinkRegex2 = GetImageLinkRegex2();
        var createTimeRegex = GetCreateTimeRegex();

        bool isImage = false;
        var currentImageFile = "";
        var blogOutputText = new StringBuilder();

        DateTime? createTime = null;

        foreach (var line in File.ReadLines(blogFile.FullName))
        {
            if (createTime is null)
            {
                var match = createTimeRegex.Match(line);
                if (match.Success)
                {
                    if (DateTime.TryParse(match.Groups[1].ValueSpan, out var time))
                    {
                        createTime = time;

<<<<<<< HEAD
                        if (createTime < new DateTime(2024, 3, 01))
=======
                        if (createTime < new DateTime(2024, 1, 1))
>>>>>>> 9c844564970decdb92c197ed43d97473064f054c
                        {
                            // 不要点爆了博客园
                            return;
                        }
                    }
                }
            }

            if (!isImage)
            {
                var match = imageFileRegex.Match(line);
                if (match.Success)
                {
                    currentImageFile = Path.Join(OriginFolder.FullName, "image", match.Groups[1].ValueSpan);
                    if (File.Exists(currentImageFile))
                    {
                        isImage = true;
                    }
                    else
                    {
                        Log.WriteLine($"本地文件找不到");
                    }
                }

                blogOutputText.AppendLine(line);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    blogOutputText.AppendLine();

                    continue;
                }

                var match = imageLinkRegex.Match(line);
                if (!match.Success)
                {
                    match = imageLinkRegex2.Match(line);
                }
                if (match.Success && !string.IsNullOrEmpty(currentImageFile) && File.Exists(currentImageFile))
                {
                    var relativePath = Path.GetRelativePath(OriginFolder.FullName, currentImageFile);

                    if (!ImageManager.TryGetImageUrl(relativePath, out var url))
                    {
                        url = CnBlogsImageUploader.UploadImage(currentImageFile);
                        ImageManager.AddImageUrl(relativePath, url);
                        Log.WriteLine($"本地文件 {currentImageFile} 上传图片");
                    }
                    else
                    {
                        Log.WriteLine($"本地文件 {currentImageFile} 命中缓存");
                    }

                    blogOutputText.AppendLine($"![]({url})");
                }
                else
                {
                    blogOutputText.AppendLine(line);
                }

                isImage = false;
                currentImageFile = null;
            }
        }

        File.WriteAllText(blogFile.FullName, blogOutputText.ToString());
    }
}