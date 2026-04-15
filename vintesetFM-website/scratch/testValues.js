import fs from 'fs';
import { processAvancadosRow, getAvancadosHeaders } from '../src/components/ferramentas/MoneyballAvancados.js';

// Parse html manually with regex to get 1 row
const htmlContent = fs.readFileSync('../moneyball_export_20260409_174409.html', 'utf8');

const tableMatch = htmlContent.match(/<table[^>]*>([\s\S]*?)<\/table>/);
if (!tableMatch) process.exit(1);

const rows = tableMatch[1].match(/<tr[^>]*>[\s\S]*?<\/tr>/g);
const headers = rows[0].match(/<th[^>]*>(.*?)<\/th>/g).map(h => h.replace(/<[^>]+>/g, '').trim());

let dataRow = rows[1]; // first player
let firstPlayerRaw = {};
let tdMatches = dataRow.match(/<td[^>]*>(.*?)<\/td>/g);
if (tdMatches) {
    tdMatches.forEach((td, i) => {
        let val = td.replace(/<[^>]+>/g, '').trim();
        firstPlayerRaw[headers[i]] = val;
    });
}

const firstPlayerCalc = processAvancadosRow(firstPlayerRaw);

console.log(`\n=== Checking anomalies for player: ${firstPlayerCalc['Jogador']} ===\n`);
for (const [key, value] of Object.entries(firstPlayerCalc)) {
    if (value === 0 || value === 0.0 || Number.isNaN(value) || value === '-' || value === '' || value === undefined || value === null || !isFinite(value)) {
        console.log(`"${key}": ${value}`);
    }
}
