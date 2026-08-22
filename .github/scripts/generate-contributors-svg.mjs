import fs from 'node:fs';
import path from 'node:path';

const AVATAR = 64;
const GAP = 4;
const MAX_COLS = 12;

const repo = process.env.GITHUB_REPOSITORY ?? process.argv[2];
const outPath = process.env.OUTPUT ?? '.github/contributors.svg';
const token = process.env.GITHUB_TOKEN ?? process.env.GH_TOKEN;

if (!repo) {
  console.error('Missing repo: set GITHUB_REPOSITORY or pass owner/repo as argv[2]');
  process.exit(1);
}

const headers = {
  Accept: 'application/vnd.github+json',
  'User-Agent': 'mursisru-contributors-svg',
  'X-GitHub-Api-Version': '2022-11-28',
};
if (token) headers.Authorization = `Bearer ${token}`;

const res = await fetch(
  `https://api.github.com/repos/${repo}/contributors?per_page=100&anon=1`,
  { headers },
);
if (!res.ok) {
  console.error(`GitHub API ${res.status}: ${await res.text()}`);
  process.exit(1);
}

const contributors = await res.json();
if (!Array.isArray(contributors) || contributors.length === 0) {
  console.error('No contributors returned from GitHub API');
  process.exit(1);
}

const cols = Math.min(MAX_COLS, contributors.length);
const rows = Math.ceil(contributors.length / cols);
const width = cols * AVATAR + Math.max(0, cols - 1) * GAP;
const height = rows * AVATAR + Math.max(0, rows - 1) * GAP;

const parts = [
  '<?xml version="1.0"?>',
  `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" role="img" aria-label="Contributors for ${repo}">`,
];

contributors.forEach((entry, index) => {
  const col = index % cols;
  const row = Math.floor(index / cols);
  const x = col * (AVATAR + GAP);
  const y = row * (AVATAR + GAP);
  const login = entry.login ?? entry.name ?? 'contributor';
  const profile = entry.login
    ? `https://github.com/${entry.login}`
    : `https://github.com/${repo}/graphs/contributors`;
  const avatar = entry.avatar_url
    ?? (entry.login ? `https://github.com/${entry.login}.png` : `https://github.com/identicons/${login}.png`);
  const clipId = `clip-${index}`;
  const cx = x + AVATAR / 2;
  const cy = y + AVATAR / 2;
  const r = AVATAR / 2;

  parts.push(`<clipPath id="${clipId}"><circle cx="${cx}" cy="${cy}" r="${r}"/></clipPath>`);
  parts.push(`<a xlink:href="${profile}" target="_blank" rel="noopener"><title>${login}</title>`);
  parts.push(
    `<image x="${x}" y="${y}" width="${AVATAR}" height="${AVATAR}" xlink:href="${avatar}" clip-path="url(#${clipId})"/>`,
  );
  parts.push('</a>');
});

parts.push('</svg>');
parts.push('');

const dir = path.dirname(outPath);
if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
fs.writeFileSync(outPath, parts.join('\n'), 'utf8');
console.log(`Wrote ${contributors.length} contributor(s) to ${outPath}`);
