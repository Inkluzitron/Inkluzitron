using Inkluzitron.Extensions;
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

        public long MsSinceUtcUnixEpoch { get; set; }

        public string Data { get; set; }

        [Required]
        public int FailCount { get; set; } = 0;

        [NotMapped]
        public DateTimeOffset When
        {
            get => DateTimeOffset.UnixEpoch.AddMilliseconds(MsSinceUtcUnixEpoch);
            set => MsSinceUtcUnixEpoch = value.ConvertDateTimeOffsetToLong();
        }

        public T ParseData<T>()
            => Data == null ? default : JsonConvert.DeserializeObject<T>(Data);
        public string Serialize()
            => JsonConvert.SerializeObject(this);
    }
}
