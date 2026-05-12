using Prodright.Objects;
using Prodright.Processing;
using System.Data;
using System.Text;
using System.Xml.Linq;

namespace Prodright
{
    public partial class Form1 : Form

    {

        private readonly IProductLookupService _products;

        public Form1(IProductLookupService products)
        {
            InitializeComponent();
            _products = products;
            SetupLayout();
        }

        private void SetupLayout()
        {
            this.Text = "SAP to Store Pack Mapper";

            dgvItems.CellFormatting += (s, e) =>
            {
                // Check if we are looking at the "Reasonableness Check" column
                if (dgvItems.Columns[e.ColumnIndex].Name == "Reasonableness Check" && e.Value != null)
                {
                    string val = e.Value.ToString();
                    if (val.Contains("❌"))
                    {
                        e.CellStyle.BackColor = System.Drawing.Color.MistyRose;
                        e.CellStyle.ForeColor = System.Drawing.Color.DarkRed;
                        e.CellStyle.Font = new System.Drawing.Font(dgvItems.Font, System.Drawing.FontStyle.Bold);
                    }
                    else if (val.Contains("⚠️"))
                    {
                        e.CellStyle.BackColor = System.Drawing.Color.LemonChiffon;
                    }
                }
            };
        }
        private void BtnLoadFiles_Click(object sender, EventArgs e)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string fileName = "MappingDiagnostics.txt";
            string outputPath = Path.Combine(baseDirectory, fileName);

            using (OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "XML Files|*.xml" })
            {
                if (ofd.ShowDialog() == DialogResult.OK && ofd.FileNames.Length >= 2)
                {
                    try
                    {
                        // Safer matching (case-insensitive)
                        string sapPath = ofd.FileNames.FirstOrDefault(f =>
                            Path.GetFileName(f).Contains("sap", StringComparison.OrdinalIgnoreCase));

                        string storePath = ofd.FileNames.FirstOrDefault(static f =>
                            Path.GetFileName(f).Contains("store", StringComparison.OrdinalIgnoreCase));

                        if (sapPath != null && storePath != null)
                        {
                            // ✅ FIX: call the "from file" overload
                            string story = SapMerchStoryBuilder.BuildStoryFromFile(sapPath);
                            rtbProductStory.Text = story;

                            txbSummary.Text = Extractions.GenerateXmlSummary(sapPath);

                            var p = new Parameters { Threshold = Convert.ToInt32(nudThreshold.Value) };
                            var data = Extractions.ParseXmlFiles(sapPath, storePath);

                            dgvItems.DataSource = Extractions.CreateMappingTable(data.conversions, data.storeItems, p);
                            dgvItems.AutoResizeColumns();
                        }
                        else
                        {
                            MessageBox.Show("Please ensure one file contains 'sap' and the other 'store' in the filename.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error processing files: {ex.Message}");
                    }
                }
            }
        }
        public void ExportXmlDiagnostics(string sapPath, string storePath, string outputPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== XML Namespace & Path Diagnostics ===");

            try
            {
                XDocument sapDoc = XDocument.Load(sapPath);
                XDocument storeDoc = XDocument.Load(storePath);
                XNamespace ns2 = "http://spar.co.za/Interface/MasterData/Global";

                // 1. Check Root and Namespace
                sb.AppendLine($"Store Root Name: {storeDoc.Root.Name}");
                sb.AppendLine($"Defined ns2: {ns2.NamespaceName}");

                // 2. Test RetailSellingPrice discovery
                var rspElements = storeDoc.Descendants(ns2 + "RetailSellingPrice").ToList();
                sb.AppendLine($"Count of 'ns2:RetailSellingPrice' found: {rspElements.Count}");

                if (rspElements.Count > 0)
                {
                    var firstRsp = rspElements.First();
                    sb.AppendLine("--- First RetailSellingPrice Element Children ---");
                    foreach (var child in firstRsp.Elements())
                    {
                        sb.AppendLine($"Child Name: {child.Name} | LocalName: {child.Name.LocalName} | Namespace: {child.Name.NamespaceName}");
                    }

                    // 3. Test PriceSpecification drill-down
                    var priceSpec = firstRsp.Element(ns2 + "PriceSpecification");
                    sb.AppendLine($"PriceSpecification found: {priceSpec != null}");

                    if (priceSpec != null)
                    {
                        var typeCode = priceSpec.Element(ns2 + "PriceSpecificationElementTypeCode");
                        sb.AppendLine($"PriceSpecificationElementTypeCode found: {typeCode != null}");
                        if (typeCode != null) sb.AppendLine($"Type Code Value: '{typeCode.Value}'");
                    }
                }

                // 4. Check SAP Conversions
                var conversions = sapDoc.Descendants("QuantityConversion").ToList();
                sb.AppendLine($"SAP QuantityConversion elements found: {conversions.Count}");

            }
            catch (Exception ex)
            {
                sb.AppendLine($"CRITICAL ERROR: {ex.Message}");
            }

            File.WriteAllText(outputPath, sb.ToString());

            // After creating the file, spawn Explorer to show the file
            try
            {
                // "explorer.exe" with /select, "path" highlights the file in its folder
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
            }
            catch (Exception ex)
            {
                // Handle cases where the process cannot be started
                MessageBox.Show($"Could not open explorer: {ex.Message}");
            }
        }
        private void btnCallApi_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "XML Files|*.xml" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Safer matching (case-insensitive)
                        string sapPath = ofd.FileNames.FirstOrDefault(f =>
                            Path.GetFileName(f).Contains("product", StringComparison.OrdinalIgnoreCase));

                        if (sapPath != null)
                        {
                            // ✅ FIX: call the "from file" overload
                            var product = Processing.SapProductStoreViewParser.FromSapAtomXml(sapPath);
                            rtbS4Product.Text = Processing.ProductStoreViewTextFormatter.ToMultilineText(product);

                        }
                        else
                        {
                            MessageBox.Show("Please ensure one file contains 'sap' and the other 'store' in the filename.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error processing files: {ex.Message}");
                    }
                }
            }
        }



        private async void buttonFetch_Click(object sender, EventArgs e)
        {
            try
            {
                var product = await _products.GetProductAsync("1020458001");
                MessageBox.Show(product?.Description ?? "No product found");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SAP not available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


    }
}
