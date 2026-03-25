import { execFileSync } from 'node:child_process';
import { mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '../..');

const inputDir = resolve(repoRoot, 'backend/src/PaymentManager.WebApi');
const outputDir = resolve(repoRoot, 'frontend/src/api-client');
const specFile = 'PaymentManager.WebApi.json';

/** Convert an absolute Windows or POSIX path to a Docker-compatible mount path. */
function toDockerPath(p: string): string {
  return p.replace(/\\/g, '/').replace(/^([A-Za-z]):/, '/$1');
}

/** Recursively delete all .ts files under a directory. */
function deleteTsFiles(dir: string): void {
  for (const entry of readdirSync(dir)) {
    const full = resolve(dir, entry);
    if (statSync(full).isDirectory()) {
      deleteTsFiles(full);
    } else if (entry.endsWith('.ts')) {
      rmSync(full);
      console.log(`  deleted: ${full}`);
    }
  }
}

const inputMount = toDockerPath(inputDir);
const outputMount = toDockerPath(outputDir);

const additionalProperties = [
  'ngVersion=21.0.0',
  'providedIn=root',
  'fileNaming=kebab-case',
  'modelPropertyNaming=original',
  'enumPropertyNaming=PascalCase',
  'supportsES6=true',
  'stringEnums=false',
  'serviceSuffix=Service',
  'serviceFileSuffix=.service',
  'npmName=payment-manager-api-client',
  'npmVersion=1.0.0',
].join(',');

console.log(`→ Input spec : ${inputDir}/${specFile}`);
console.log(`→ Output dir : ${outputDir}`);
console.log('');

mkdirSync(outputDir, { recursive: true });

console.log('→ Removing existing .ts files from output directory...');
deleteTsFiles(outputDir);
console.log('');

const dockerArgs = [
  'run',
  '--rm',
  '-v',
  `${inputMount}:/local/input:ro`,
  '-v',
  `${outputMount}:/local/output`,
  'openapitools/openapi-generator-cli:latest',
  'generate',
  '--input-spec',
  `/local/input/${specFile}`,
  '--generator-name',
  'typescript-angular',
  '--output',
  '/local/output',
  `--additional-properties=${additionalProperties}`,
];

execFileSync('docker', dockerArgs, { stdio: 'inherit' });

const generatedTsConfig = resolve(outputDir, 'tsconfig.json');
const tsConfig = JSON.parse(readFileSync(generatedTsConfig, 'utf-8'));
tsConfig.compilerOptions.moduleResolution = 'bundler';
writeFileSync(generatedTsConfig, JSON.stringify(tsConfig, null, 4) + '\n');
console.log('→ Patched moduleResolution to "bundler" in generated tsconfig.json');

console.log(`\n✔ API client generated at: ${outputDir}`);
