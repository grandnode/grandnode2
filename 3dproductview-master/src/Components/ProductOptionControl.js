// src/Components/ProductOptionControl.js 
import React, { Component } from 'react'
import {TweenMax, Power2} from "gsap"

class ProductOptionControl extends Component {

  constructor(props) {
    super(props)
    // Set initial state
    this.state = {
    	bodyDefaultHeight: null,   
    	accordionOpen: false  	
    }
    /*
    * Bind event to this class. We will use this method in a
    * click event listener and this is to keep the 
    * correct scope.
    */
    this.toggleAccordion = this.toggleAccordion.bind(this)
    window.addEventListener('accordion-open', () => {
      if(this.state.accordionOpen) {
        TweenMax.to(this.optionBody, 0.05, {height: 0, ease: Power2.easeOut})
        this.setState({accordionOpen: false})
        this.optionBody.style.visibility = 'hidden'        
      }
    })
  }

  componentDidMount() {
     /* 
     * We record the components initial height so it can be used 
     * make the accordion open and close. This way we know 
     * exactly how tall it needs to be.
     */
     this.setState({bodyDefaultHeight: this.optionBody.clientHeight})
     // Then we set its height to 0 to close it.
     this.optionBody.style.height = 0
     this.optionBody.style.visibility = 'hidden'
  }

  /*
  * Add a new method to notify our scene that it
  * needs to move the camera when a control
  * is opened so we can get a better view
  * of the area our control is editing.
  */
  emitSectionChangeEvent() {
    // Arguments: Event Name, Event Options
  	let event = new CustomEvent('move-camera', { detail: this.props.optionName })
    // Triggers the event on the window object
  	window.dispatchEvent(event)
  }

  emitColorChangeEvent(color) {
  	let event = new CustomEvent('change-color', { detail: {
  		meshName: this.props.optionName,
  		color
  	}})
  	window.dispatchEvent(event)
  }

  emitAccordionOpenEvent(color) {
    let event = new CustomEvent('accordion-open')
    window.dispatchEvent(event)
  }

  toggleAccordion(e) {
    /*
    * When a user clicks the the title of the control we 
    * want to open the accordion, unless it is already
    * open in which case we want to close it
    */
  	if(!this.state.accordionOpen) {
      /*
      *  To open the accordion we set its height to the original
      *  value we stored when the component mounted
      */
      this.setState({accordionOpen: true})
      this.optionBody.style.visibility = 'visible'
      TweenMax.to(this.optionBody, 0.05, {height: this.state.bodyDefaultHeight, ease: Power2.easeOut})
      this.emitAccordionOpenEvent()
      /*
      * Call our new method here in the accordion 
      * event hook
      */
      this.emitSectionChangeEvent()
  	} else {
      // back to 0 to close 
      TweenMax.to(this.optionBody, 0.05, {height: 0, ease: Power2.easeOut})
  		this.setState({accordionOpen: false})
      this.optionBody.style.visibility = 'hidden'
  	}
  }

  render() {
    return (       
    	<div className="productoption" >
          {/*
          *  The accordion header, this is always visible. We attach a click
          *  event handler to it to handle opening and closing of 
          *  the accordion body
          */}
	        <button onClick={this.toggleAccordion} className="productoption_header" >
	        	{this.props.optionName}
	        </button> 
          {/*
          *  We capture another reference to a DOM element here. 
          *  This is to read and modify the height so we 
          *  can make our accordion
          */}      
	        <div className="productoption_body" ref={ el => this.optionBody = el} >
             {/* Map over all the possible options and list them out */}
	        	{this.props.optionValues.map( value => {
	        		return <button key={value} onClick={(e) => {
	        			e.preventDefault()
	        			this.emitColorChangeEvent(value)
	        		}} className="productoption_btn" >{value}</button>
	        	})}
	        </div>        
        </div>
    )
  }
}

export default ProductOptionControl
