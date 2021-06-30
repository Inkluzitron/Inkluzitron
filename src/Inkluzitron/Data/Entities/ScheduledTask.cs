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

        public long MsSinceUtcUnixEpoch { get; set; }

        public string Data { get; set; }

        [Required]
        public int FailCount { get; set; } = 0;

        [NotMapped]
        public DateTimeOffset When
        {
            get => DateTimeOffset.UnixEpoch.AddMilliseconds(MsSinceUtcUnixEpoch);
            set => MsSinceUtcUnixEpoch = ConvertDateTimeOffset(value);
        }

        public T ParseData<T>()
            => Data == null ? default : JsonConvert.DeserializeObject<T>(Data);

        static public long ConvertDateTimeOffset(DateTimeOffset value)
        {
            if (value < DateTimeOffset.UnixEpoch)
                throw new ArgumentException("The value is too far in the past.", nameof(value));

            return (long)Math.Round((value - DateTimeOffset.UnixEpoch).TotalMilliseconds);
        }

        public string Serialize()
            => JsonConvert.SerializeObject(this);
    }
}
