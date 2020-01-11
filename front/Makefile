CREDENTIAL_FILE=./credentials/firebase-client.json

install:
	yarn install

init: FIREBASE_CREDENTIAL=
init:
	npx cpx $(FIREBASE_CREDENTIAL) $(CREDENTIAL_FILE)

build/development:
	npx webpack --mode development

build/watch:
	npx webpack --mode development --watch

build/production:
	npx webpack --mode production

build/serve:
	npx webpack-dev-server --hot-only --history-api-fallback --mode development --open
