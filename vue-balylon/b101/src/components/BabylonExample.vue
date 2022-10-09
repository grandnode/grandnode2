<template>
  <div>
    <h3>Babylon Examples</h3>
    <canvas ref="canvas"></canvas>
    <button class="changeColor" @click="changeColor">Change Color</button>
    <button class="changeCamera" @click="changeCamera">Change Camera</button>
    <button class="changeTexure" @click="changeTexture">Change Texture</button>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import { BasicScene } from "@/BabylonExamples/BasicScene";

export default defineComponent({
  name: "BabylonExamples",
  data() {
    return {
      currentColorData: "none",
    };
  },
  mounted() {
    new BasicScene(this.$refs.canvas as HTMLCanvasElement);
  },
  methods: {
    changeColor: function () {
      this.currentColorData = "red";
      console.log("change color click");
      window.dispatchEvent(new Event("changeColorEvent", new Object(
        {
          "color": this.currentColorData
        }
      )));
      
    },
    changeCamera: function(){
      window.dispatchEvent(new Event("changeCameraEvent"));
    },
    changeTexture: function(){
      window.dispatchEvent(new Event("changeTextureEvent"));
    }
  },
});
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
canvas {
  width: 70%;
  height: 70%;
}
</style>
