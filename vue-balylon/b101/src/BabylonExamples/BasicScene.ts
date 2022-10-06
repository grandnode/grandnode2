import { ArcRotateCamera, AssetsManager, AxesViewer, Engine, FreeCamera, HemisphericLight, MeshBuilder, Scene, Tools, Vector2, Vector3 } from "@babylonjs/core";

export class BasicScene{
    scene: Scene;
    engine: Engine;
    
    constructor(private canvas: HTMLCanvasElement){
        // this.setEngine(canvas);

        this.engine = new Engine(canvas, true);
        this.scene = new Scene(this.engine);   


        this.setCamera();
        this.setLight();
        // this.setMesh();    
        // this.engine.runRenderLoop(() =>{
            // this.scene.render();
        // })
        this.loadModel().load();
    }

    setCamera(){
        const camera = new ArcRotateCamera("Camera", Math.PI * 2, Tools.ToRadians(80), 15, new Vector3(0,3,-3), this.scene);
        camera.setTarget(new Vector3(0, 1,0));
        camera.attachControl();        
    }

    setLight(){
        const hemiLight = new HemisphericLight("hemiLight", new Vector3(0,1,0), this.scene);
    }

    setMesh(){
        const ground = MeshBuilder.CreateGround("ground", {width: 10, height: 10}, this.scene);
        const ball = MeshBuilder.CreateSphere("ball", {diameter:1}, this.scene);
        ball.position = new Vector3(0,1,0);
    }

    loadModel(){
        const loader = new AssetsManager(this.scene);
        const loadHelmetModel = loader.addMeshTask("helmet", "", "", "12318_Helmet_v1_L3.obj");
        loadHelmetModel.onSuccess= (t)=>{
            this.engine.runRenderLoop(() =>{
                this.scene.render();
            })
        }

        return loader;
    }


}