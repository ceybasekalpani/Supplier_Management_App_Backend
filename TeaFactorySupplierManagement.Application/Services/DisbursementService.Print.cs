using System.Text;

namespace TeaFactorySupplierManagement.Application.Services
{
    public partial class DisbursementService
    {
        public async Task<string?> BuildDeliveryNotePrintHtmlAsync(int id)
        {
            var note = await GetDeliveryNoteAsync(id);
            if (note == null)
            {
                return null;
            }

            var rows = new StringBuilder();
            for (var i = 0; i < note.Details.Count; i++)
            {
                var detail = note.Details[i];
                var amountOrQty = detail.Amount.HasValue
                    ? $"Rs. {detail.Amount.Value:N2}"
                    : $"{detail.Quantity:N2} {detail.Unit}".Trim();

                rows.AppendLine($"""
                    <tr>
                        <td>{i + 1}</td>
                        <td>{EscapeHtml(detail.SupplierRegNo.ToString())}</td>
                        <td>{EscapeHtml(detail.SupplierName)}</td>
                        <td>{EscapeHtml(ToDisplayIssuedType(detail.ItemType))}</td>
                        <td>{EscapeHtml(amountOrQty)}</td>
                        <td class="signature"></td>
                    </tr>
                    """);
            }

            return $$"""
                <!doctype html>
                <html>
                <head>
                    <meta charset="utf-8" />
                    <title>Delivery Note {{EscapeHtml(note.DeliveryNoteNo)}}</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 32px; color: #111; }
                        h1 { text-align: center; margin-bottom: 8px; }
                        .meta { display: grid; grid-template-columns: 1fr 1fr; gap: 8px 24px; margin: 24px 0; }
                        table { width: 100%; border-collapse: collapse; margin-top: 16px; }
                        th, td { border: 1px solid #333; padding: 8px; text-align: left; }
                        th { background: #f3f3f3; }
                        .signature { height: 36px; }
                        .footer { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 24px; margin-top: 48px; }
                        .line { border-top: 1px solid #111; padding-top: 6px; text-align: center; }
                    </style>
                </head>
                <body>
                    <h1>Delivery Note / Gate Pass</h1>
                    <div class="meta">
                        <div><strong>DN No:</strong> {{EscapeHtml(note.DeliveryNoteNo)}}</div>
                        <div><strong>Date:</strong> {{EscapeHtml(note.DispatchDate)}}</div>
                        <div><strong>Borrower:</strong> {{EscapeHtml(note.BorrowerName)}}</div>
                        <div><strong>Role:</strong> {{EscapeHtml(note.BorrowerRole)}}</div>
                        <div><strong>Vehicle No:</strong> {{EscapeHtml(note.VehicleNo ?? string.Empty)}}</div>
                        <div><strong>Route:</strong> {{EscapeHtml(note.RouteName ?? string.Empty)}}</div>
                    </div>
                    <table>
                        <thead>
                            <tr>
                                <th>No</th>
                                <th>Supplier Reg No</th>
                                <th>Supplier Name</th>
                                <th>Type</th>
                                <th>Amount / Qty</th>
                                <th>Signature</th>
                            </tr>
                        </thead>
                        <tbody>{{rows}}</tbody>
                    </table>
                    <div class="footer">
                        <div class="line">Issued By</div>
                        <div class="line">Borrower Signature</div>
                        <div class="line">Checked By</div>
                    </div>
                </body>
                </html>
                """;
        }

        private static string ToDisplayIssuedType(string value)
        {
            return value switch
            {
                "advance" => "Cash Advance",
                "fertilizer" => "Fertilizer",
                "items" => "Item",
                _ => value
            };
        }

        private static string EscapeHtml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}
