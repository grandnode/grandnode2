import { Animation, ArcRotateCamera, AssetsManager, AxesViewer, BaseTexture, Camera, Color3, CubicEase, EasingFunction, Engine, FreeCamera, HemisphericLight, InternalTexture, InternalTextureSource, Light, Material, MeshBuilder, PBRSpecularGlossinessMaterial, PointLight, Scene, StandardMaterial, Texture, Tools, Vector2, Vector3 } from "@babylonjs/core";
import { ThinSprite } from "@babylonjs/core/Sprites/thinSprite";
import 'babylonjs-loaders';

export class BasicScene{
    scene: Scene;
    engine: Engine;
    camera: ArcRotateCamera;
    light: HemisphericLight;
    
    constructor(private canvas: HTMLCanvasElement){
        this.engine = new Engine(canvas, true);
        this.scene = new Scene(this.engine);  
        // Camera
        // alpha là quay trái quay phải. 
        // Math.PI là chính diện, lớn hơn là quay phải, nhỏ hơn là quay trái
        // beta - quay len quay  xuống, lớn là nhìn từ dưới lên, nhỏ là nhìn từ trên xuống 
        this.camera = new ArcRotateCamera("Camera", Math.PI*0.9, Tools.ToRadians(60), 60, new Vector3(20,10,-3), this.scene);
        this.camera.setTarget(new Vector3(0, 20,0));
        this.camera.attachControl()
        // end camera
        //Light
        this.light = new HemisphericLight("hemiLight", new Vector3(0, 40, 0), this.scene);

        // end light
        this.loadModel().load();
    }

    setMesh(){
        const ground = MeshBuilder.CreateGround("ground", {width: 10, height: 10}, this.scene);
        const ball = MeshBuilder.CreateSphere("ball", {diameter:1}, this.scene);
        ball.position = new Vector3(0,1,0);
    }

    loadModel(){
        const loader = new AssetsManager(this.scene);
        const loadHelmetModel = loader.addMeshTask("helmetTask", ["helmet", "lot", "lotcam", "quai", "quainum", "quaisau"], "", "helmet.babylon");
        loadHelmetModel.onSuccess= (t)=>{
            this.setEventListener();
            this.engine.runRenderLoop(() =>{
                this.scene.render();
            })
        }
         
        return loader;
    }
    
    setEventListener(){
        window.addEventListener("changeColorEvent", (event) =>{
            const lot = this.scene.getMeshByName("helmet")!;
            const material = new StandardMaterial("material", this.scene);
            // How light refect on the bumpy surface
            //  material.diffuseColor = new Color3(102,0,102);
            // How light reflect on the smooth surface
            //material.specularColor = new Color3(102, 0, 102)
            // ???
            material.emissiveColor = new Color3(102,0,102);
            //material.ambientColor = new Color3(102, 0, 102);
            lot.material = material;
        })

        window.addEventListener("changeCameraEvent", (event) => {
            console.log("change Camera");
            const newBeta = Tools.ToRadians(80);
            
            this.camera.animations = [
                this.createAnimation({
                    property: "beta",
                    from: this.camera.beta,
                    to: newBeta
                })

            ]

            this.scene.beginAnimation(this.camera, 0, 100, false, 4);

        })

        window.addEventListener("changeTextureEvent", (event) =>{
            console.log("change texture");
            const texture = new StandardMaterial("materialTexture", this.scene);

            texture.specularTexture = new Texture("sonnhung.jpg", this.scene);
            texture.diffuseTexture = new Texture("sonnhung.jpg", this.scene);
            const helmet = this.scene.getMeshByName("helmet")!;
            helmet.material = texture;
        })
    }

    createAnimation( {property, from, to}: {property: string; from: number; to: number}){
        const ease = new CubicEase();
        ease.setEasingMode(EasingFunction.EASINGMODE_EASEINOUT);
        const Frame_per_second = 60;
        const animation = Animation.CreateAnimation(
            property,
            Animation.ANIMATIONTYPE_FLOAT,
            Frame_per_second,
            ease
        );


        animation.setKeys([
            {
                frame: 0,
                value: from
            },
            {
                frame: 100,
                value: to
            }
        ])

        return animation;
    }

}