import { ArcRotateCamera, AssetsManager, AxesViewer, Color3, Engine, FreeCamera, HemisphericLight, Material, MeshBuilder, Scene, StandardMaterial, Tools, Vector2, Vector3 } from "@babylonjs/core";
import { ThinSprite } from "@babylonjs/core/Sprites/thinSprite";
import 'babylonjs-loaders';

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
        this.setMeshColor();
        this.loadModel().load();
        // this.setMeshColor()
    }

    setCamera(){
        const camera = new ArcRotateCamera("Camera", Math.PI * 2, Tools.ToRadians(80), 40, new Vector3(10,3,-3), this.scene);
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
        const loadHelmetModel = loader.addMeshTask("helmetTask", "helmet", "", "helmet.babylon");
        loadHelmetModel.onSuccess= (t)=>{
            
            this.engine.runRenderLoop(() =>{

                this.scene.render();
            })
        }

        return loader;
    }
    
    setMeshColor(){
            const helmet = this.scene.getMeshByName("helmet")!;

            const material = new StandardMaterial("material", this.scene);
            material.diffuseColor = new Color3(1,0,1);
            material.specularColor = new Color3(0.5, 0.6, 0.87);
            material.emissiveColor = new Color3(1,1,1);
            material.ambientColor = new Color3(0.23, 0.98, 0.53);
            helmet.material = material;
    }

}