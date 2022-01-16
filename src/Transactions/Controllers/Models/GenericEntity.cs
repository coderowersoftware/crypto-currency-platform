using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Transactions.Controllers.Models
{
    /// <summary>
    /// GenericEntity
    /// </summary>
    [DataContract]
    public partial class GenericEntity : GenericModel, IEquatable<GenericEntity>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericEntity" /> class.
        /// </summary>
        /// <param name="importHash">importHash.</param>
        /// <param name="tenantId">tenantId.</param>
        public GenericEntity(string? importHash = default(string), Guid? tenantId = default(Guid?), Guid? id = default(Guid?), string? createdAt = default(string), string? updatedAt = default(string), string? deletedAt = default(string), Guid? createdById = default(Guid?), Guid? updatedById = default(Guid?)) : base(id, createdAt, updatedAt, deletedAt, createdById, updatedById)
        {
            this.ImportHash = importHash;
            this.TenantId = tenantId;
        }

        /// <summary>
        /// Gets or Sets ImportHash
        /// </summary>
        [DataMember(Name = "importHash", EmitDefaultValue = false)]
        public string? ImportHash { get; set; }

        /// <summary>
        /// Gets or Sets TenantId
        /// </summary>
        [DataMember(Name = "tenantId", EmitDefaultValue = false)]
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class GenericEntity {\n");
            sb.Append("  ").Append(base.ToString().Replace("\n", "\n  ")).Append("\n");
            sb.Append("  ImportHash: ").Append(ImportHash).Append("\n");
            sb.Append("  TenantId: ").Append(TenantId).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public override string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object? input)
        {
            return this.Equals(input as GenericEntity);
        }

        /// <summary>
        /// Returns true if GenericEntity instances are equal
        /// </summary>
        /// <param name="input">Instance of GenericEntity to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(GenericEntity? input)
        {
            if (input == null)
                return false;

            return base.Equals(input) &&
                (
                    this.ImportHash == input.ImportHash ||
                    (this.ImportHash != null &&
                    this.ImportHash.Equals(input.ImportHash))
                ) && base.Equals(input) &&
                (
                    this.TenantId == input.TenantId ||
                    (this.TenantId != null &&
                    this.TenantId.Equals(input.TenantId))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = base.GetHashCode();
                if (this.ImportHash != null)
                    hashCode = hashCode * 59 + this.ImportHash.GetHashCode();
                if (this.TenantId != null)
                    hashCode = hashCode * 59 + this.TenantId.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}