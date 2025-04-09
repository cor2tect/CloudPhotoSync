using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CloudPhotoSync.Service
{

    public class FileObjectStore : IObjectStore
    {
        private readonly string rootDirectory;

        private readonly ILogger<FileObjectStore> logger;

        public FileObjectStore(
            FileStorageOptions options,
            ILogger<FileObjectStore> logger
        )
        {
            this.rootDirectory = options.RootDirectory;
            this.logger = logger;
        }

        private string GetPath(string relPath) =>
            Path.Combine(
                rootDirectory,
                relPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            );

        private static async Task<byte[]> GetMd5HashAsync(string file, CancellationToken ct)
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var md5 = System.Security.Cryptography.MD5.Create();
            return await md5.ComputeHashAsync(fs, ct);
        }

        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(File.Exists(GetPath(path)));
        }

        public async Task<ObjectMetaData> GetMetaDataAsync(string path)
        {
            var file = new FileInfo(GetPath(path));
            if (!file.Exists) throw new Exception($"File does not exist: {path}");

            var hash = await GetMd5HashAsync(file.FullName, CancellationToken.None);
            var lastWrite = file.LastWriteTimeUtc;
            var length = (int)file.Length;

            return new ObjectMetaData(
                Hash: hash,
                LastWrite: lastWrite,
                Path: path,
                Length: length
            );
        }

        public async IAsyncEnumerable<string> GetPaths(string prefix, IEnumerable<string> folders)
        {
            string GetRelativePath(string path) =>
                path.Replace(rootDirectory + Path.DirectorySeparatorChar, "");

            foreach (var folder in folders)
            {
                await foreach (var file in Directory
                    .EnumerateFiles(
                        rootDirectory,
                        prefix + "*",
                        new EnumerationOptions()
                        {
                            RecurseSubdirectories = true
                        }
                    )
                    .Where(f => f.Contains($"/{folder}/") ||
                                (folder == "/" &&
                                 Regex.IsMatch(f[..f.LastIndexOf('/')].Split(new[] { @"/" }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty, @".*-.*-[\d]{8,8}-[\d]{1,}")))
                    .AsParallel()
                    .ToAsyncEnumerable()
                    .Select(GetRelativePath))
                    yield return file;
            }
        }

        public IAsyncEnumerable<ObjectMetaData> GetMetaDataSet(string prefix, IEnumerable<string> folders)
        {
            async ValueTask<ObjectMetaData> GetMeta(string path) =>
                await GetMetaDataAsync(path);

            return GetPaths(prefix, folders).SelectAwait(GetMeta);
        }

        public Task<Stream> ReadAsync(string path)
        {
            var stream = File.OpenRead(GetPath(path));
            return Task.FromResult<Stream>(stream);
        }

        public async Task<long> WriteAsync(string path, Stream source)
        {
            var fi = new FileInfo(GetPath(path));
            var di = fi.Directory!;
            if (!di.Exists) di.Create();

            await using var target = fi.OpenWrite();
            await source.CopyToAsync(target);
            return target.Length;
        }

        public Task DeleteAsync(string path)
        {
            var fPath = GetPath(path);
            File.Delete(fPath);

            // If the directory is empty, we can remove it
            var directory = Path.GetDirectoryName(fPath);
            if (directory == null) return Task.CompletedTask;
            if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
            {
                Directory.Delete(directory, false);
                logger.LogDebug($"Deleted folder: {directory}");
                // If the parent directory is empty we can delete it too
                var parentDirectory = Directory.GetParent(directory);
                if (parentDirectory == null) return Task.CompletedTask;
                if (Directory.GetFiles(parentDirectory.FullName).Length == 0 &&
                    Directory.GetDirectories(parentDirectory.FullName).Length == 0)
                {
                    Directory.Delete(parentDirectory.FullName, false);
                    logger.LogDebug($"Deleted parent folder: {parentDirectory}");
                }

                return Task.CompletedTask;

            }
            return Task.CompletedTask;
        }
    }
}
