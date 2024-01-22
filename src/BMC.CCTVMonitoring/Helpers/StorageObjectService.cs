using BMC.CCTVMonitoring.Models;
using IDCH.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BMC.CCTVMonitoring.Helpers
{
    public class StorageObjectService 
    {
        Blobs _Blobs;

        public static bool TestSetting(StorageSetting input)
        {
            try
            {
                input.GenerateBaseUrl();
                var setting = new IDCHSettings(
                           input.EndpointUrl,
                           input.Ssl,
                           input.AccessKey,
                           input.SecretKey,
                           input.Region,
                           input.Bucket,
                           input.BaseUrl
                           );
                var blob = new Blobs(setting);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            return true;
           
        }
        public StorageObjectService(StorageSetting input)
        {
            input.GenerateBaseUrl();
            var setting = new IDCHSettings(
                       input.EndpointUrl,
                       input.Ssl,
                       input.AccessKey,
                       input.SecretKey,
                       input.Region,
                       input.Bucket,
                       input.BaseUrl
                       );
            _Blobs = new Blobs(setting);
           
        }
        public async Task<bool> DeleteData(string Key)
        {
            try
            {
                await _Blobs.Delete(Key);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            
        }

        
        public async Task<List<StorageObject>> GetAllData()
        {
            var datas = new List<StorageObject>();

            try
            {
                var result = await _Blobs.Enumerate();
                foreach(var res in result?.Blobs)
                {
                    datas.Add(new StorageObject() { 
                     ContentType = res.ContentType, Size = res.ContentLength, LastAccess = res.LastAccessUtc, LastUpdate = res.LastUpdateUtc, Name = res.Key, FileUrl = _Blobs.GenerateUrl(res.Key)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return datas;

        }

        public async Task<StorageObject> GetDataByKey(string Key)
        {
            try
            {
                var res  = await _Blobs.GetMetadata(Key);
                if (res != null)
                {
                    return new StorageObject()
                    {
                        ContentType = res.ContentType,
                        Size = res.ContentLength,
                        LastAccess = res.LastAccessUtc,
                        LastUpdate = res.LastUpdateUtc,
                        Name = res.Key,
                        FileUrl = _Blobs.GenerateUrl(res.Key)
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return default;
        } 
        
        public async Task<(string Name, string ContentType, byte[] Data)> DownloadByKey(string Key)
        {
            try
            {
                var res = await _Blobs.GetMetadata(Key);
                if (res != null)
                {
                    var meta =  new StorageObject()
                    {
                        ContentType = res.ContentType,
                        Size = res.ContentLength,
                        LastAccess = res.LastAccessUtc,
                        LastUpdate = res.LastUpdateUtc,
                        Name = res.Key,
                        FileUrl = _Blobs.GenerateUrl(res.Key)
                    };
                    var data = await _Blobs.Get(Key);
                    if (data != null)
                    {
                        return (meta.Name, meta.ContentType,data);
                    }
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return ("empty","empty",default);
        }

     

        public async Task<bool> InsertData(string Key, string ContentType, byte[] Data)
        {
            try
            {
                await _Blobs.Write(Key,ContentType,Data);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public async Task< bool> UpdateData(string Key, string ContentType, byte[] Data)
        {
            try
            {
                await _Blobs.Write(Key, ContentType, Data);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public async Task<byte[]> GetFile(string filename)
        {
            try
            {
                /*
                var res = await blob.DownloadFile(fileName);
                var result = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(res)
                };
                result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                */

                //var file = await blob.DownloadFile(filename);
                var item = await DownloadByKey(filename);
                var file = item.Data;
                if (file != null)
                {
                    return file;
                }
            }
            catch (Exception)
            {
            }
            return default;
        }

        public async Task<bool> UploadFile(string fileName, byte[] Data)
        {
            try
            {
                /*
                if (!string.IsNullOrEmpty(DocFolder))
                {
                    var targetFile = $"{DocFolder}/{fileName}";
                    File.WriteAllBytes(targetFile, Data);
                }*/
                var ext = MimeTypeHelper.GetMimeType(Path.GetExtension(fileName));
                var res = await InsertData(fileName, ext, Data);
                //get Blob reference

                //CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                //cloudBlockBlob.Properties.ContentType = imageToUpload.ContentType;

                //await cloudBlockBlob.UploadFromByteArrayAsync(Data, 0, Data.Length);
                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
