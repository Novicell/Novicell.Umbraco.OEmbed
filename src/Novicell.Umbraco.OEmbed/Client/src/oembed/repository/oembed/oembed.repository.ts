import { OEmbedServerDataSource } from './oembed.server.data-source.ts';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import type { UmbApi } from '@umbraco-cms/backoffice/extension-api';

export class OEmbedRepository extends UmbControllerBase implements UmbApi {
    readonly #dataSource = new OEmbedServerDataSource(this);

    constructor(host: UmbControllerHost) {
        super(host);
    }

    async requestOEmbed({ url, maxWidth, maxHeight }: { url?: string; maxWidth?: number; maxHeight?: number }) {
        const { data, error } = await this.#dataSource.getOEmbed({ url, maxWidth, maxHeight });
        if (!error) {
            return { data };
        }
        return { error };
    }
}

export { OEmbedRepository as api };