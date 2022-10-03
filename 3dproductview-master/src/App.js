// src/App.js 
import React, { Component } from 'react'
import './App.css'
import Scene3d from './Components/Scene3d'
import Controls from './Components/Controls'

class App extends Component {

  constructor(props) {
    super(props)
    this.state = {
      optionNames: [ 'frame', 'seat', 'waterbottle', 'handlebars' ],   
      optionValues: [
        [ 'white', 'red', 'blue', 'green' ],
        [ 'grey', 'white', 'black' ],
        [ 'grey', 'white', 'black' ],
        [ 'grey', 'white', 'black' ]
      ]
    }
  }

  render() {
    return (
      <div className="App">                
        <Scene3d />                
        <Controls options={this.state.optionNames} optionValues={this.state.optionValues} />        
      </div>
    )
  }
}

export default App
