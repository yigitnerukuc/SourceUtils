﻿/// <reference path="PagedLoader.ts"/>

namespace SourceUtils {
    import WebGame = Facepunch.WebGame;

    export interface ILeafFace {
        material: number;
        element: number;
    }

    export interface IMaterialGroup {
        materialUrl: string;
        meshData: WebGame.ICompressedMeshData;
    }

    export interface ILeafGeometryPage {
        leaves: ILeafFace[][];
        materials: IMaterialGroup[];
    }

    export class LeafGeometryPage extends ResourcePage<ILeafGeometryPage, WebGame.MeshHandle[]> {
        private readonly viewer: MapViewer;

        private matGroups: WebGame.MeshHandle[][];
        private leafFaces: ILeafFace[][];

        constructor(viewer: MapViewer, page: IPageInfo) {
            super(page);

            this.viewer = viewer;
        }

        onLoadValues(page: ILeafGeometryPage): void {
            this.matGroups = new Array<WebGame.MeshHandle[]>(page.materials.length);
            this.leafFaces = page.leaves;

            for (let i = 0, iEnd = page.materials.length; i < iEnd; ++i) {
                const matGroup = page.materials[i];
                const mat = this.viewer.materialLoader.load(matGroup.materialUrl);
                const data = WebGame.MeshManager.decompress(matGroup.meshData);
                this.matGroups[i] = this.viewer.meshes.addMeshData(data, index => mat);
            }

            super.onLoadValues(page);
        }

        protected onGetValue(index: number): Facepunch.WebGame.MeshHandle[] {
            const leafFaces = this.leafFaces[index];

            const handles = new Array<WebGame.MeshHandle>(leafFaces.length);
            for (let i = 0, iEnd = leafFaces.length; i < iEnd; ++i) {
                const leafFace = leafFaces[i];
                handles[i] = this.matGroups[leafFace.material][leafFace.element];
            }

            return handles;
        }
    }

    export class LeafGeometryLoader extends PagedLoader<LeafGeometryPage, ILeafGeometryPage, WebGame.MeshHandle[]> {
        readonly viewer: MapViewer;

        constructor(viewer: MapViewer) {
            super();

            this.viewer = viewer;
        }

        protected createPage(page: IPageInfo): LeafGeometryPage {
            return new LeafGeometryPage(this.viewer, page);
        }
    }
}