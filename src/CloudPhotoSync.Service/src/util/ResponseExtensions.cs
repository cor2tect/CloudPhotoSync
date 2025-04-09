using System.Threading.Tasks;

namespace Azure
{
    public static class ResponseExtensions
    {
        public static async Task<T> GetValueAsync<T>(this Task<Response<T>> responseTask)
        {
            var response = await responseTask.ConfigureAwait(false);
            return response.Value;
        }
    }

}
