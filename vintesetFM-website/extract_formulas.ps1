$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
try {
   $wb = $excel.Workbooks.Open('C:\Users\Raphael\Downloads\Allan FCL - Moneyball FM26 (1)\1. Planilha - Moneyball\Moneyball FM26 - Avancados.xlsm')
   $sheet = $wb.Sheets.Item(2) # Assuming index 2 is Avançados based on our previous run where index 1 was Time Estatísticas ? Wait, let's search by name
   foreach ($s in $wb.Sheets) {
       if ($s.Name -match "Avan") {
           $sheet = $s
       }
   }
   
   Write-Output "Extracting columns for $($sheet.Name)"
   
   $headersRows = $sheet.Range("A2:DZ2").Value2
   $formulaRow = $sheet.Range("A3:DZ3")
   $vals = $formulaRow.Value2

   for ($c = 1; $c -le 80; $c++) {
       $h = $headersRows[1, $c]
       if ($h) {
           $cell = $sheet.Cells.Item(3, $c)
           $formula = $cell.Formula
           $val = $vals[1, $c]
           Write-Output "Col $c ($h): F[$formula] V[$val]"
       }
   }
} finally {
   $wb.Close($false)
   $excel.Quit()
}
