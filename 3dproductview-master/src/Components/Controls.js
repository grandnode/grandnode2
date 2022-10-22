// src/Components/Controls.js
import React, { Component } from 'react'
// The Controls component will import out ProductOptionControl
import ProductOptionControl from './ProductOptionControl'

class Controls extends Component {


  render() {
    return (       
        <div className="controls" >
        {/* We are going to take our options as an array of props.*/}
        {this.props.options.map( (option, cnt) =>  {
          {/* We'll render an inidvidual control for each item in the array */}
          return <ProductOptionControl key={option} optionName={option} optionValues={this.props.optionValues[cnt]} />
        })}
        </div>        
    )
  }
}

export default Controls
