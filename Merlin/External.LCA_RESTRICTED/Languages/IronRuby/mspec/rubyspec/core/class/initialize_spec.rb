require File.dirname(__FILE__) + '/../../spec_helper'

describe "Class#new" do
  it "returns a new instance of self" do
    klass = Class.new
    klass.new.is_a?(klass).should == true
  end
  
  it "invokes #initialize on the new instance with the given args" do
    klass = Class.new do
      def initialize(*args)
        @initialized = true
        @args = args
      end
      
      def args
        @args
      end
      
      def initialized?
        @initialized || false
      end
    end
    
    klass.new.initialized?.should == true
    klass.new(1, 2, 3).args.should == [1, 2, 3]
  end

  it "is private on classes" do
    klass = Class.new do
      def initialize
        1
      end
    end

    #TODO: replace with private method matcher when RubySpec is updated
    klass.method(:initialize).should be_kind_of Method
    klass.private_methods.should include("initialize")
    lambda { klass.initialize }.should raise_error(NoMethodError)
  end
end
