import { execSync } from 'node:child_process';
import { mkdirSync } from 'node:fs';
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
  'withInterfaces=false',
  'npmName=payment-manager-api-client',
  'npmVersion=1.0.0',
].join(',');

console.log(`→ Input spec : ${inputDir}/${specFile}`);
console.log(`→ Output dir : ${outputDir}`);
console.log('');

mkdirSync(outputDir, { recursive: true });

const command = [
  'docker run --rm',
  `-v "${inputMount}:/local/input:ro"`,
  `-v "${outputMount}:/local/output"`,
  'openapitools/openapi-generator-cli:latest generate',
  `--input-spec /local/input/${specFile}`,
  '--generator-name typescript-angular',
  '--output /local/output',
  `--additional-properties=${additionalProperties}`,
].join(' ');

execSync(command, { stdio: 'inherit' });

console.log(`\n✔ API client generated at: ${outputDir}`);
