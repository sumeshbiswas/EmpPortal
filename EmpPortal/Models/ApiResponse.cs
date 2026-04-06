namespace travelexpensemanagement.Models
{
    public class ApiResponse<T>
    {
        public bool status { get; set; }
        public T data { get; set; }
        public string message { get; set; }
    }
}
