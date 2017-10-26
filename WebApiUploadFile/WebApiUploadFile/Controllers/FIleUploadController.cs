using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace WebApiUploadFile.Controllers
{
    public class FIleUploadController : ApiController
    {
        const string StoragePath = @"D:\360Downloads\";

        public HttpResponseMessage GetFileByName(string fileName)
        {
            string filePath = Path.Combine(StoragePath, fileName);
            //从图片中读取byte  
            var imgByte = File.ReadAllBytes(filePath);
            //从图片中读取流  
            var imgStream = new MemoryStream(File.ReadAllBytes(filePath));
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(imgByte)
                //或者  
                //Content = new StreamContent(stream)  
            };
            string mimeType = MimeMapping.GetMimeMapping(fileName);//获取mime type
            resp.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);//设置mime
            resp.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("Inline");
            resp.Content.Headers.ContentDisposition.FileName = fileName;
            return resp;
        }
        public async Task<HttpResponseMessage> PostFile()
        {
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string root = HttpContext.Current.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);

            try
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                StringBuilder sb = new StringBuilder(); // Holds the response body

                // Read the form data and return an async task.
                await Request.Content.ReadAsMultipartAsync(provider);

                // This illustrates how to get the form data.
                foreach (var key in provider.FormData.AllKeys)
                {
                    //foreach (var val in provider.FormData.GetValues(key))
                    //{
                    //    sb.Append(string.Format("{0}: {1}\n", key, val));

                    //}
                    dic.Add(key, provider.FormData[key]);
                }

                // This illustrates how to get the file names for uploaded files.
                foreach (var file in provider.FileData)
                {
                    FileInfo fileInfo = new FileInfo(file.LocalFileName);
                    sb.Append(string.Format("Uploaded file: {0} ({1} bytes)\n", fileInfo.Name, fileInfo.Length));
                    
                    if (string.IsNullOrEmpty(file.Headers.ContentDisposition.FileName))
                    {
                        return Request.CreateResponse(HttpStatusCode.NotAcceptable, "This request is not properly formatted");
                    }
                    string fileName = file.Headers.ContentDisposition.FileName;
                    if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                    {
                        fileName = fileName.Trim('"');
                    }
                    if (fileName.Contains(@"/") || fileName.Contains(@"\"))
                    {
                        fileName = Path.GetFileName(fileName);
                    }
                    string filePath = Path.Combine(StoragePath, fileName);
                    //Deletion exists file  
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                   
                    fileInfo.CopyTo(filePath, true);
                    dic.Add("Name",fileName);
                    dic.Add("filePath", StoragePath + fileName);
                    dic.Add("url",HttpContext.Current.Request.Url.AbsoluteUri+ "?fileName="+fileName);
                }
                var jSetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                var  resp = new HttpResponseMessage { Content = new StringContent(JsonConvert.SerializeObject(dic, Formatting.Indented, jSetting), System.Text.Encoding.UTF8, "application/json") };
                return resp;
                //return new HttpResponseMessage()
                //{
                //    Content = new StringContent(sb.ToString())
                //};
            }
            catch (System.Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }

    }
}
