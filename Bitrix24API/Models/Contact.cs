using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Bitrix24API.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
    }
    public class ContactField
    {
        public string VALUE { get; set; }
        public string VALUE_TYPE { get; set; }
    }
}
