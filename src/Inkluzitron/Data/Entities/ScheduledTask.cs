using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkluzitron.Data.Entities
{
    public class ScheduledTask
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ScheduledTaskId { get; set; }

        [Required]
        public string Discriminator { get; set; }

        public string Tag { get; set; }

        public string Data { get; set; }

        public int FailCount { get; set; } = 0;

        public DateTimeOffset When { get; set; }

        public T ParseData<T>()
            => Data == null ? default : JsonConvert.DeserializeObject<T>(Data);

        public string Serialize()
            => JsonConvert.SerializeObject(this);
    }
}
