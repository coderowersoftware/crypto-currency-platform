using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CodeRower.CCP.Controllers.Models
{
    /// <summary>
    /// TransactionType
    /// </summary>
    [DataContract]
    public partial class AutoCompleteResponse : GenericEntity, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionType" /> class.
        /// </summary>
        /// <param name="identifier">identifier.</param>
        /// <param name="name">name.</param>
        /// <param name="isActive">isActive.</param>
        public AutoCompleteResponse(string? id = default(string), string? label = default(string))
        {            
            this.Id = id;
            this.Label = label;
        }

    /// <summary>
    /// Gets or Sets Identifier
    /// </summary>
    [DataMember(Name = "id", EmitDefaultValue = false)]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or Sets Name
    /// </summary>
    [DataMember(Name = "label", EmitDefaultValue = false)]
    public string? Label { get; set; }


    /// <summary>
    /// Returns the string presentation of the object
    /// </summary>
    /// <returns>String presentation of the object</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("class AutoCompleteResponse {\n");
        sb.Append("  ").Append(base.ToString().Replace("\n", "\n  ")).Append("\n");
        sb.Append("  Id: ").Append(Id).Append("\n");
        sb.Append("  Label: ").Append(Label).Append("\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    /// <summary>
    /// Returns the JSON string presentation of the object
    /// </summary>
    /// <returns>JSON string presentation of the object</returns>
    public virtual string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

}
}