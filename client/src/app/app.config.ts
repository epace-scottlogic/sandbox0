import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideRpcClient } from '../rpc';
import { environment } from '../environments/environment';
import { BlockchainMethods } from '../blockchain/models/blockchain-methods';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideRpcClient<BlockchainMethods>({
      hubUrl: environment.blockchainHubUrl,
    }),
  ],
};
